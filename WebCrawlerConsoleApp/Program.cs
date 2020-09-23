using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var git = new GitCrawlerClient(@"https://github.com");
            var google = new GoogleCrawlerClient(@"https://www.google.com");

            git.OnError += ((Exception ex) => {
                Console.WriteLine(ex.ToString());
            });

            google.OnError += ((Exception ex) => {
                Console.WriteLine(ex.ToString());
            });

            try
            {
                Console.WriteLine("Consulta de projetos no GitHub");
                foreach (var projeto in await git.ListarProjetos())
                    Console.WriteLine($"Projeto: {projeto}");

                Space(3);

                await git.Conectar("raywall.malheiros@gmail.com", "");

                Space(3);

                Console.WriteLine("Realizando download do avatar no GitHub");
                Console.WriteLine(await git.GetProfileImnage(Environment.CurrentDirectory));

                Space(3);

                Console.WriteLine("Consulta resultados no Google");
                foreach (var resultado in google.BuscarUrlPor("itau"))
                    Console.WriteLine(resultado);

                Space(1);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void Space(int lenght)
        {
            for (var c = 0; c < lenght; c++) 
                Console.WriteLine(string.Empty);
        }
    }
}
