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
    public static Armi ScegliArma()
{
    Console.WriteLine("Scegli un'arma iniziale:\n");
    Console.WriteLine("1. Spada  2. Scudo  3. Coltello");
    while (true)
    {
        string arma = Console.ReadLine() ?? "";
            switch (arma.ToLower())
            {
                case "1" or "spada":
                    return Armi.Spada();
                case "2" or "scudo":
                    return Armi.Scudo();
                case "3" or "coltello":
                    return Armi.coltello();
                default:
                    Console.WriteLine("Input invalido");
                    break;
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

    public static void MostraDanno(string nome, int danno)
    {
        Console.WriteLine($"{nome} prende {danno} danni!");
    }
    public static string Input(Giocatore g)
    {
        Console.WriteLine($"\n{g.Nome}>");
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

    public static void EntrataNemico(Nemico n)
    {
        Console.WriteLine($"Davanti a te compare un {n.Nome}!");
    }

    public static void MostraTurnoGiocatore(Giocatore g, Combattimento combat)
    {
        Console.WriteLine($"TURNO DI {g.Nome.ToUpper()}\n");
        Console.WriteLine("Azioni:");
        
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

//  Aggiungere: Testo per uscita combattimento,
//  Testo per mostrare le azioni,
//  in Program.cs cambiare la logica Azioni Nemici,
//  in Program.cs la logica per usare l'oggetto (e i corrispondenti testi stampati),
