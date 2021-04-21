using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CriadoresCaes_tA_B.Data;
using CriadoresCaes_tA_B.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace CriadoresCaes_tA_B.Controllers
{

    public class FotografiasController : Controller
    {

        /// <summary>
        /// este atributo representa a base de dados do projeto
        /// </summary>
        private readonly CriadoresCaesDB _context;

        /// <summary>
        /// este atributo contém os dados 
        /// </summary>
        private readonly IWebHostEnvironment _caminho;

        public FotografiasController(CriadoresCaesDB context, IWebHostEnvironment caminho)
        {
            _context = context;
            _context = caminho;
        }

        // GET: Fotografias
        public async Task<IActionResult> Index()
        {

            /* criação de uma variável que vai conter um conjunto de dados
            * vindos da base de dados
            * se fosse em SQL, a pesquisa seria:
            *     SELECT *
            *     FROM Fotografias f, Caes c
            *     WHERE f.CaoFK = c.Id
            *  exatamente equivalente a _context.Fotografias.Include(f => f.Cao), feita em LINQ
            *  f => f.Cao  <---- expressão 'lambda'
            *  ^ ^  ^
            *  | |  |
            *  | |  representa cada um dos registos individuais da tabela das Fotografias
            *  | |  e associa a cada fotografia o seu respetivo Cão
            *  | |  equivalente à parte WHERE do comando SQL
            *  | |
            *  | um símbolo que separa os ramos da expressão
            *  |
            *  representa todos registos das fotografias
            */
            var fotografias = _context.Fotografias.Include(f => f.Cao);

            // invoca a View, entregando-lhe a lista de registos
            return View(await fotografias.ToListAsync());
        }




        // GET: Fotografias/Details/5
        /// <summary>
        /// Mostra os detalhes de uma fotografia
        /// </summary>
        /// <param name="id">Identificador da Fotografia</param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                // entro aqui se não foi específicado o ID

                // redirecionar para a página de início
                return RedirectToAction("Index");

                //return NotFound();
            }

            // se chego aqui, foi específicado um ID
            // vou procurar se existe uma Fotografia com esse valor

            var fotografia = await _context.Fotografias
                .Include(f => f.Cao)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (fotografia == null)
            {
                // o ID específicado não corresponde a uma fotografia
                //return NotFound();
                // redirecionar para a página de início
                return RedirectToAction("Index");
            }

            // se cheguei aqui, é porque a foto existe e foi encontrada
            //mostro-a na View

            return View(fotografia);
        }

        // GET: Fotografias/Create
        /// <summary>
        /// 
        ///invoca, na primeira vez, a View com os dados de criação de uma fotografia
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {

            // geração da lista de valores disponíveis na DropDown
            // o ViewData transporta dados a serem associados ao atributo 'CaoFK'
            // 
            ViewData["CaoFK"] = new SelectList(_context.Caes.OrderBy(c=>c.Nome), "Id", "Nome");
            return View();
        }

        // POST: Fotografias/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DataFoto,Local,CaoFK")] Fotografias fotografias, IFormFile fotoCao)

        /* processar o ficheiro
         * - existe ficheiro?
         *  - se não existe, o que fazer? => gerar uma mensagem de erro e devolver o controlo à View
         *  - se continua é porque o ficheiro existe
         *      - mas será que é do tipo correto?
         *          - avaliar se é imagem
         *              - se sim: - especificar o seu novo nome 
         *                  - específicar a localização
         *                  - associar ao objeto 'foto' o nome deste ficheiro
         *                  - guardar ficheiro no disco rígido do servidor 
         *              - se não => gerar uma mensagem de erro e devolver o controlo à View
        */
        
        
        
        {
            //var auxiliar
            string nomeImagem = "";

            if (fotoCao == null)
            {
                // não há ficheiro
                // adicionar msg de erro
                ModelState.AddModelError("", "Adicione, por favor, a fotografia do cão");
                ViewData["CaoFK"] = new SelectList(_context.Caes.OrderBy(c=> c.Nome), "Id", "Nome");
                // devolver o controlo à View
                return View(fotografias);
            }
            else
            {
                // há ficheiro. Mas será um ficheiro válido?
                if (fotoCao .ContentType == "image/jpeg" || fotoCao .ContentType == "image/png")
                {
                    //definir o novo nome da foto da fotografia 
                    Guid g;
                    g = Guid.NewGuid();
                    nomeImagem = fotografias.CaoFK + "_" + g.ToString(); // tambem poderia ser usado a formatação
                    // determinar a extensão do nome da imagem
                    string extensao = Path.GetExtension(fotoCao.FileName).ToLower();
                    // agora, consigo ter o nome final do ficheiro
                    nomeImagem = nomeImagem+extensao;

                    //associar este ficheiro aos dados da Fotografia do cão
                    fotografias.Fotografia = nomeImagem;

                    //localização do amazenamento da imagem
                    string localizacaoFicheiro = _caminho.WebRootPath;
                    nomeImagem = Path.Combine(localizacaoFicheiro,"fotos", nomeImagem);
                }
                else
                {
                    // ficheiro não é válido
                    // adicionar msg de erro
                    ModelState.AddModelError("", "Só pode escolher uma imagem para a associar ao cão");
                    ViewData["CaoFK"] = new SelectList(_context.Caes.OrderBy(c => c.Nome), "Id", "Nome");
                    // devolver o controlo à View
                    return View(fotografias);
                }
            }

            if (ModelState.IsValid)
            {
                // Adicionar os dados da nova fotografia à base de dados
                _context.Add(fotografias);
                // Consolidar os dados na base de dados
                await _context.SaveChangesAsync();

                // Se cheguei aqui, tudo correu bem
                //Vou guardar, agora, no disco rígido do Servidor a imagem
                using var stream = new FileStream(nomeImagem, FileMode.Create);
                await fotoCao.CopyToAsync(stream);

                return RedirectToAction(nameof(Index));
            }
            ViewData["CaoFK"] = new SelectList(_context.Caes, "Id", "Id", fotografias.CaoFK);
            return View(fotografias);
        }

        // GET: Fotografias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fotografias = await _context.Fotografias.FindAsync(id);
            if (fotografias == null)
            {
                return NotFound();
            }
            ViewData["CaoFK"] = new SelectList(_context.Caes, "Id", "Id", fotografias.CaoFK);
            return View(fotografias);
        }

        // POST: Fotografias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Fotografia,DataFoto,Local,CaoFK")] Fotografias fotografias)
        {
            if (id != fotografias.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fotografias);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FotografiasExists(fotografias.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CaoFK"] = new SelectList(_context.Caes, "Id", "Id", fotografias.CaoFK);
            return View(fotografias);
        }

        // GET: Fotografias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fotografias = await _context.Fotografias
                .Include(f => f.Cao)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fotografias == null)
            {
                return NotFound();
            }

            return View(fotografias);
        }

        // POST: Fotografias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fotografias = await _context.Fotografias.FindAsync(id);
            try
            {
                //Proteger a eliminação de uma foto
                _context.Fotografias.Remove(fotografias);
                await _context.SaveChangesAsync();

                // não esquecer, remover o ficheiro da Fotografia do disco rígido

            }
            catch(Exception)
            {
                throw;
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool FotografiasExists(int id)
        {
            return _context.Fotografias.Any(e => e.Id == id);
        }
    }
}
