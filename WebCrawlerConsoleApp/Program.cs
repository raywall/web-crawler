﻿using System;
using System.Threading.Tasks;

namespace WebCrawlerConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var git = new GitCrawlerClient(@"https://github.com");
            var caixa = new CaixaCrawlerClient(@"http://loterias.caixa.gov.br/");
            var google = new GoogleCrawlerClient(@"https://www.google.com");
            
            git.OnError += ((Exception ex) => {
                Console.WriteLine(ex.ToString());
            });

            caixa.OnError += ((Exception ex) => {
                Console.WriteLine(ex.ToString());
            });

            google.OnError += ((Exception ex) => {
                Console.WriteLine(ex.ToString());
            });

            try
            {
                Console.WriteLine("Consulta de projetos no GitHub");
                foreach (var projeto in git.ListarProjetos())
                    Console.WriteLine($"Projeto: {projeto}");

                Space(3);

                Console.WriteLine("Consulta de projetos no GitHub (logado)");
                if (git.Conectar("<email de acesso ao seu github>", "<senha do seu github>"))
                    foreach (var projeto in git.ListarProjetos())
                        Console.WriteLine($"Projeto: {projeto}");

                Space(3);

                Console.WriteLine("Realizando download do avatar no GitHub");
                Console.WriteLine(git.GetProfileImnage(Environment.CurrentDirectory));

                Space(3);

                Console.WriteLine("Extraindo resultado da MegaSena no site da Caixa");
                var result = caixa.Megasena();

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
