using System;
namespace ProgEsameUniVPM;
public static class UI
{
    /* private enum StatoInit {
        ScriviNome,
        PrendiArma,
        Conferma,
        FineInit
    }
    private static StatoInit stato; */
    public static string ChiediNome()
    {
        Console.WriteLine("Inserisci il nome del tuo personaggio: \n");
        while(true){
            string nome = Console.ReadLine() ?? "";
            if((nome == "") || (nome.Length > 16))
            {
                Console.WriteLine("Nome invalido.");
            }
            else
            {
                return nome;
            }
        }
    }

    public static void MostraStanza(Stanza s)
    {
        Console.WriteLine($"===={s.Nome.ToUpper()}====");
        Console.WriteLine($"Piano: {s.Livello}");
        Console.WriteLine($"{s.Descrizione}");
        Console.WriteLine($"Azioni (digita per eseguire): " + string.Join("\n", s.Azioni.Keys));
    }

    public static string Input(Giocatore g)
    {
        Console.WriteLine($"\n{g.Nome}>");
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

    public static void GameOver(Giocatore g)
    {
        Console.WriteLine($"\n===FINE===\n{g.Nome} è morto/a, premi un tasto per chiudere il gioco.");
        Console.ReadKey();
        Environment.Exit(0);
    }

    public static void MostraErrore(string s)
    {
        Console.WriteLine("ERRORE: " + s);
    }
}