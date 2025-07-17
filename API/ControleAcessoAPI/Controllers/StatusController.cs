using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControleAcessoAPI.Data;
using ControleAcessoAPI.Models;

namespace ControleAcessoAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class StatusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatusController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("status")]
        public async Task<IActionResult> ReceberStatus([FromBody] StatusModel status)
        {
            var computador = await _context.Computadores.FirstOrDefaultAsync(c => c.Nome == status.Computador);
            if (computador == null)
            {
                computador = new Computador
                {
                    Nome = status.Computador,
                    Usuario = status.Usuario,
                    IpAtual = status.IpAtual,
                    UltimaVerificacao = status.DataHora,
                    EstaNaEmpresa = status.EstaNaEmpresa,
                    HomeOfficeAutorizado = false
                };
                _context.Computadores.Add(computador);
            }
            else
            {
                computador.Usuario = status.Usuario;
                computador.IpAtual = status.IpAtual;
                computador.UltimaVerificacao = status.DataHora;
                computador.EstaNaEmpresa = status.EstaNaEmpresa;
            }

            _context.StatusLogs.Add(new StatusLog
            {
                Computador = status.Computador,
                Usuario = status.Usuario,
                IpAtual = status.IpAtual,
                EstaNaEmpresa = status.EstaNaEmpresa,
                DataHora = status.DataHora
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("verifica-permissao")]
        public async Task<IActionResult> VerificarPermissao([FromQuery] string computador)
        {
            var comp = await _context.Computadores.FirstOrDefaultAsync(c => c.Nome == computador);
            if (comp == null)
                return NotFound(false);

            return Ok(comp.HomeOfficeAutorizado);
        }

        [HttpGet("computadores")]
        public async Task<IActionResult> GetComputadores()
        {
            var lista = await _context.Computadores.ToListAsync();
            return Ok(lista);
        }

        [HttpPost("atualizar-permissoes")]
        public async Task<IActionResult> AtualizarPermissoes([FromBody] List<Computador> computadores)
        {
            foreach (var comp in computadores)
            {
                var existente = await _context.Computadores.FirstOrDefaultAsync(c => c.Nome == comp.Nome);
                if (existente != null)
                {
                    existente.HomeOfficeAutorizado = comp.HomeOfficeAutorizado;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}
