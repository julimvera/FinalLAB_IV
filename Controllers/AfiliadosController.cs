using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using Tickets_MVC.Data;
using Tickets_MVC.Models;
using Microsoft.AspNetCore.Hosting;
namespace WebApp_Tickets.Controllers
{
    public class AfiliadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadsFolder = Path.Combine("wwwroot", "uploads");

        public AfiliadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Afiliados
        public async Task<IActionResult> Index(string? nombre, string? apellido, int page = 1)
        {
            const int PageSize = 5;
            var query = _context.Afiliados.AsQueryable();

            if (!string.IsNullOrEmpty(nombre)) query = query.Where(a => a.Nombres.Contains(nombre));
            if (!string.IsNullOrEmpty(apellido)) query = query.Where(a => a.Apellido.Contains(apellido));

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            var result = await query
                .OrderBy(a => a.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["Nombre"] = nombre;
            ViewData["Apellido"] = apellido;

            return View(result);
        }

        // GET: Afiliados/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var afiliado = await _context.Afiliados.FindAsync(id);
            if (afiliado == null) return NotFound();

            return View(afiliado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Afiliado afiliado)
        {
            if (id != afiliado.Id) return BadRequest();

            
                var afiliadoExistente = await _context.Afiliados.FindAsync(id);
                if (afiliadoExistente == null) return NotFound();

               
                afiliadoExistente.Apellido = afiliado.Apellido;
                afiliadoExistente.Nombres = afiliado.Nombres;
                afiliadoExistente.DNI = afiliado.DNI;
                afiliadoExistente.FechaNacimiento = afiliado.FechaNacimiento;

                try
                {
                    _context.Update(afiliadoExistente);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Afiliado actualizado correctamente.";
                }
                catch
                {
                    TempData["ErrorMessage"] = "Error al actualizar el afiliado.";
                }

                return RedirectToAction(nameof(Index));
            

            return View(afiliado);
        }

        // GET: Afiliados/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var afiliado = await _context.Afiliados.FindAsync(id);
            if (afiliado == null) return NotFound();

            return View(afiliado);
        }

        // POST: Afiliados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var afiliado = await _context.Afiliados.FindAsync(id);
            if (afiliado == null) return NotFound();

            try
            {
                _context.Afiliados.Remove(afiliado);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Afiliado eliminado correctamente.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error al eliminar el afiliado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Afiliados/ImportarDesdeExcel
        [Authorize]
        public IActionResult ImportarDesdeExcel()
        {
            return View();
        }

        // POST: Afiliados/ImportarDesdeExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ImportarDesdeExcel(IFormFile archivoExcel)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; //lo tuve que poner porque sino me daba error al importar
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                TempData["ErrorMessage"] = "No se ha seleccionado un archivo válido.";
                return RedirectToAction(nameof(ImportarDesdeExcel));
            }

            using (var memoryStream = new MemoryStream())
            {
                await archivoExcel.CopyToAsync(memoryStream);

                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int totalRows = worksheet.Dimension.Rows;

                    for (int row = 2; row <= totalRows; row++)
                    {
                        var apellido = worksheet.Cells[row, 1].Text;
                        var nombres = worksheet.Cells[row, 2].Text;
                        var dni = worksheet.Cells[row, 3].Text;
                        var fechaNacimientoStr = worksheet.Cells[row, 4].Text;
                        var foto = worksheet.Cells[row, 5].Text;

                        if (string.IsNullOrEmpty(apellido) || string.IsNullOrEmpty(nombres) || string.IsNullOrEmpty(dni))
                            continue;

                        var afiliado = new Afiliado
                        {
                            Apellido = apellido,
                            Nombres = nombres,
                            DNI = dni,
                            Foto = foto
                        };

                        if (DateTime.TryParse(fechaNacimientoStr, out DateTime fechaNacimiento))
                            afiliado.FechaNacimiento = fechaNacimiento;

                        _context.Afiliados.Add(afiliado);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Afiliados importados con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Afiliados/CargarManualmente
        [Authorize]
        public IActionResult CargarManualmente()
        {
            return View();
        }
        //mi codigo anterior
        //// POST: Afiliados/CargarManualmente
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CargarManualmente(Afiliado afiliado, IFormFile? archivo)
        {

            if (archivo != null && archivo.Length > 0)
            {
                var uploadsPath = Path.Combine("wwwroot/uploads");

                
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                
                var fileName = Path.GetFileName(archivo.FileName);
                var fullPath = Path.Combine(uploadsPath, fileName);

                
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                
                afiliado.Foto = "/uploads/" + fileName;
            }

            
            _context.Add(afiliado);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Afiliado cargado correctamente.";
            return RedirectToAction(nameof(Index));
        }


    }


}

