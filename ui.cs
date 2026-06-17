using System;
namespace ProgEsameUniVPM;

/// <summary>
/// Classe statica per gestire tutto l'I/O a console: input dell'utente
/// (normalizzato in minuscolo con Trim), output di stanze, combattimenti,
/// menù e messaggi di sistema.
/// </summary>
public static class UI
{
    /// <summary>
    /// Chiede all'utente se desidera caricare un salvataggio esistente.
    /// Accetta "s"/"si"/"sì"/"yes"/"y" per sì, "n"/"no" per no.
    /// </summary>
    /// <returns><c>true</c> se l'utente vuole caricare, <c>false</c> altrimenti.</returns>
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

    /// <summary>
    /// Richiede il nome del personaggio. Rifiuta nomi vuoti o più lunghi di 16 caratteri.
    /// </summary>
    /// <returns>Il nome scelto dal giocatore.</returns>
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

    /// <summary>
    /// Mostra il menù di scelta dell'arma iniziale e restituisce l'arma selezionata.
    /// </summary>
    /// <returns>L'arma scelta (Spada, Scudo o Coltello).</returns>
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

    /// <summary>
    /// Mostra le informazioni della stanza corrente: nome, livello, descrizione e azioni disponibili.
    /// </summary>
    /// <param name="s">Stanza da mostrare.</param>
    public static void MostraStanza(Stanza s)
    {
        Console.WriteLine($"===={s.Nome.ToUpper()}====");
        Console.WriteLine($"Piano: {s.Livello}");
        Console.WriteLine($"{s.Descrizione}");
        Console.WriteLine();
        Console.WriteLine("=== AZIONI DISPONIBILI ===");
        MostraAzioni(s);
    }

    /// <summary>
    /// Mostra la lista degli oggetti in vendita dal mercante.
    /// </summary>
    /// <param name="oggetti">Lista degli oggetti disponibili all'acquisto.</param>
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

    public static void ListaInventario(Stack<Oggetto> inv)
    {
        
    }

    /// <summary>
    /// Elenca tutte le azioni disponibili nella stanza con ID e descrizione.
    /// </summary>
    /// <param name="s">Stanza di cui mostrare le azioni.</param>
    public static void MostraAzioni(Stanza s)
    {
        if (s.Azioni.Count == 0)
        {
            Console.WriteLine("  Nessuna azione disponibile.");
            return;
        }
        
        foreach (var azione in s.Azioni.Values)
        {
            Console.WriteLine($"  {azione.Id,-20} - {azione.Descrizione}");
        }
        Console.WriteLine("drop - fai cadere l'ultimo oggetto messo nell'inventario.");
        Console.WriteLine("guarda inventario - guarda l'ultimo oggetto messo");
        Console.WriteLine();
    }

    /// <summary>
    /// Mostra un messaggio di danno inflitto a un'entità.
    /// </summary>
    /// <param name="nome">Nome dell'entità che subisce il danno.</param>
    /// <param name="danno">Quantità di danno subito.</param>
    public static void MostraDanno(string nome, int danno)
    {
        Console.WriteLine($"{nome} prende {danno} danni!");
    }

    /// <summary>
    /// Richiede un input all'utente, mostrando il nome del giocatore come prompt.
    /// L'input viene normalizzato (Trim + ToLower).
    /// </summary>
    /// <param name="g">Giocatore corrente (per il prompt).</param>
    /// <returns>Input normalizzato dell'utente.</returns>
    public static string Input(Giocatore g)
    {
        Console.WriteLine($"\n{g.Nome}>");
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

    /// <summary>
    /// Mostra il messaggio di entrata in combattimento di un nemico.
    /// </summary>
    /// <param name="n">Nemico apparso.</param>
    public static void EntrataNemico(Nemico n)
    {
        Console.WriteLine($"Davanti a te compare un {n.Nome}!");
    }

    /// <summary>
    /// Mostra l'interfaccia del turno del giocatore durante il combattimento:
    /// HP, stamina, stato del nemico e azioni disponibili.
    /// </summary>
    /// <param name="g">Giocatore corrente.</param>
    /// <param name="nemico">Nemico affrontato.</param>
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

    /// <summary>
    /// Mostra il messaggio di vittoria dopo aver sconfitto un nemico, inclusa la ricompensa in oro.
    /// </summary>
    /// <param name="n">Nemico sconfitto.</param>
    /// <param name="oro">Oro ottenuto come ricompensa.</param>
    public static void MostraVittoria(Nemico n, int oro)
    {
        Console.WriteLine($"\nHai sconfitto {n.Nome}!");
        Console.WriteLine($"Hai ottenuto {oro} oro.");
    }

    /// <summary>
    /// Tenta la fuga dal combattimento con una probabilità del 40%.
    /// </summary>
    /// <returns><c>true</c> se la fuga riesce, <c>false</c> altrimenti.</returns>
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

    /// <summary>
    /// Mostra la schermata di Game Over e termina il gioco.
    /// </summary>
    /// <param name="g">Giocatore sconfitto.</param>
    public static void GameOver(Giocatore g)
    {
        Console.WriteLine($"\n===FINE===\n{g.Nome} è morto/a, premi un tasto per chiudere il gioco.");
        Console.ReadKey();
        Environment.Exit(0);
    }

    /// <summary>
    /// Mostra un messaggio di errore a schermo.
    /// </summary>
    /// <param name="s">Testo dell'errore.</param>
    public static void MostraErrore(string s)
    {
        Console.WriteLine("ERRORE: " + s);
    }

    /// <summary>
    /// Mostra un messaggio generico a schermo.
    /// </summary>
    /// <param name="s">Testo del messaggio.</param>
    public static void MostraMessaggio(string s)
    {
        Console.WriteLine(s);
    }

    /// <summary>
    /// Mostra l'animazione testuale del teletrasporto verso una destinazione.
    /// </summary>
    /// <param name="nomeDestinazione">Nome della stanza di destinazione.</param>
    public static void MostraTeletrasporto(string nomeDestinazione)
    {
        Console.WriteLine("Il cerchio runico si illumina! Vieni avvolto da una luce accecante...");
        Console.WriteLine($"...e riappaiono nella {nomeDestinazione}.");
    }
}
