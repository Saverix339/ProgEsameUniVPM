using System;
namespace ProgEsameUniVPM;

//Classe statica per gestire tutto quello che viene stampato a schermo. 
//Gestisce inoltre la formattazione dell'input in '.Trim().ToLower()'
public static class UI
{
    public static bool ChiediCaricamento()
    {
        Console.WriteLine("È stato trovato un salvataggio precedente. Desideri caricarlo? (s/n)");
        while (true)
        {
            string input = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (input is "s" or "si" or "sì" or "yes" or "y")
            {
                return true;
            }
            if (input is "n" or "no")
            {
                return false;
            }
            Console.WriteLine("Input non valido. Inserisci 's' per sì o 'n' per no.");
        }
    }

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
        Console.WriteLine();
        Console.WriteLine("=== AZIONI DISPONIBILI ===");
        MostraAzioni(s);
    }

    public static void ListaOggettiMercante(List<Oggetto> oggetti)
    {
        if(oggetti.Count() == 0) {Console.WriteLine("Con il tavolo vuoto, il mercante se ne è andato."); return;}
        Console.WriteLine("Nella stanza trovi un tavolo con sopra vari oggetti in vendita.\n Dietro il banco risiede una misteriosa\n");
        Console.WriteLine("figura incappucciata. Ti osserva in silenzio.");
        Console.WriteLine("Puoi comprare i seguenti oggetti (digita il nome per acquistare, 'esci' per andartene):");
        foreach(var ogg in oggetti)
        {
            Console.WriteLine($"{ogg.Nome,-20}-{ogg.Valore} oro\n");
        }
    }
    public static void MostraAzioni(Stanza s)
    {
        if (s.Azioni.Count == 0)
        {
            Console.WriteLine("  Nessuna azione disponibile.");
            return;
        }
        // var gruppi = s.Azioni.Values.GroupBy(a => a.Categoria);
        // foreach (var gruppo in gruppi)
        // {
        //     Console.WriteLine($"\n[{gruppo.Key}]");
        //     foreach (var azione in gruppo)
        //     {
        //         Console.WriteLine($"  {azione.Id,-20} - {azione.Descrizione}");
        //     }
        // }
        foreach (var azione in s.Azioni.Values)
        {
            Console.WriteLine($"  {azione.Id,-20} - {azione.Descrizione}");
        }
        Console.WriteLine();
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

    public static void MostraTurnoGiocatore(Giocatore g, Nemico nemico)
    {
        Console.WriteLine($"TURNO DI {g.Nome.ToUpper()}");
        Console.WriteLine($"HP: {g.PuntiVita}/{g.PuntiVitaMax}  Stamina: {g.Stamina}/{g.StaminaMax}");
        Console.WriteLine($"Nemico: {nemico.Nome} (HP: {nemico.Salute}/{nemico.SaluteMax})");
        Console.WriteLine("\nAzioni disponibili:");
        Console.WriteLine("  attacca - attacca il nemico");
        Console.WriteLine("  abilita  - usa l'abilità dell'arma");
        Console.WriteLine("  usa      - usa un consumabile");
        Console.WriteLine("  scappa   - tenta di fuggire dal combattimento");
    }

    public static void MostraVittoria(Nemico n, int oro)
    {
        Console.WriteLine($"\nHai sconfitto {n.Nome}!");
        Console.WriteLine($"Hai ottenuto {oro} oro.");
    }

    public static bool ChiediFuga()
    {
        Console.WriteLine("Tentativo di fuga...");
        Random rand = new Random();
        bool success = rand.Next(100) < 40;
        if (success)
            Console.WriteLine("Sei riuscito a fuggire!");
        else
            Console.WriteLine("Non sei riuscito a fuggire!");
        return success;
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
    //Semplicemente mostra un messaggio 's'.
    //Per rendere il codice ordinato, dovrebbe essere usato poco (invece, bisognerebbe creare metodi apposta per i casi a cui servono)
    public static void MostraMessaggio(string s)
    {
        Console.WriteLine(s);
    }

    public static void MostraTeletrasporto(string nomeDestinazione)
    {
        Console.WriteLine("Il cerchio runico si illumina! Vieni avvolto da una luce accecante...");
        Console.WriteLine($"...e riappaiono nella {nomeDestinazione}.");
    }
}

//  Aggiungere: Testo per uscita combattimento,
//  Testo per mostrare le azioni,
//  in Program.cs cambiare la logica Azioni Nemici,
//  in Program.cs la logica per usare l'oggetto (e i corrispondenti testi stampati),
