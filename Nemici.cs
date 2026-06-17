using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

/// <summary>
/// Rappresenta un nemico combattibile nel gioco. Implementa <see cref="IDannegiabile"/> per ricevere danni/cure,
/// e possiede una lista di <see cref="AbilitaNemico"/> con sistema di pesi probabilistici per la selezione automatica.
/// </summary>
public class Nemico : IDannegiabile
{
    /// <summary>Nome del nemico.</summary>
    public required string Nome {get; set;}
    /// <summary>Descrizione testuale del nemico.</summary>
    public string Descrizione = "";
    /// <summary>Punti salute attuali.</summary>
    public int Salute {get; set;}
    /// <summary>Punti salute massimi.</summary>
    public int SaluteMax;

    /// <summary>Lista delle abilità che il nemico può usare in combattimento. Non serializzabile.</summary>
    [JsonIgnore]
    public List<AbilitaNemico> Abilita = new();

    /// <summary>Somma totale dei pesi di probabilità di tutte le abilità.</summary>
    private int totPeso;

    /// <summary>Lista degli status effect attualmente attivi sul nemico. Non serializzabile.</summary>
    [JsonIgnore]
    public List<StatusEffect> statusEffects = new();

    /// <summary>
    /// Infligge danni al nemico. Se la salute scende a 0 o meno, il nemico è sconfitto.
    /// </summary>
    /// <param name="danno">Quantità di danno da infliggere.</param>
    public void Danneggia(int danno, IDannegiabile? n = null)
    {
        Salute -= danno;
        Logger.Get<Nemico>().LogDebug("{Nemico} subisce {Danno} danni (HP: {HP}/{Max})", Nome, danno, Salute, SaluteMax);
        if(Salute <= 0)
        {
            Logger.Get<Nemico>().LogInformation("{Nemico} sconfitto", Nome);
        }
        UI.MostraDanno(Nome, danno);
    }

    /// <summary>
    /// Cura il nemico di una quantità, senza superare la salute massima.
    /// </summary>
    /// <param name="cura">Quantità di cure da applicare.</param>
    public void Cura(int cura)
    {
        Salute += cura;
        if(Salute > SaluteMax)
        {
            Salute = SaluteMax;
        }
        Logger.Get<Nemico>().LogDebug("{Nemico} curato di {Cura} (HP: {HP}/{Max})", Nome, cura, Salute, SaluteMax);
    }

    /// <summary>
    /// Crea un'istanza preconfigurata del nemico Mimic.
    /// </summary>
    /// <returns>Un nuovo Mimic pronto per il combattimento.</returns>
    public static Nemico Mimic()
    {
        var s = new Nemico()
        {
            Nome = "Mimic",
            Descrizione = "Una creatura mutaforma camuffato come cassa del tesoro, fai attenzione ai suoi denti aguzzi.",
            Salute = 80,
            SaluteMax = 80,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Morso",
                    Danno = 10,
                    PesoProbabilita = 1,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan, nem);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Cura",
                    PesoProbabilita = 1,
                    CondizioneSpeciale = (gioc, nem) => nem.Salute < (nem.SaluteMax /2),
                    EffettiSpeciali = (gioc, nem) => nem.Cura(20)
                }
            }
        };
        s.Pesa();
        return s;
    }

    /// <summary>
    /// Crea un'istanza preconfigurata del Maestro d'Armi (miniboss).
    /// </summary>
    /// <returns>Un nuovo Maestro d'Armi pronto per il combattimento.</returns>
    public static Nemico MaestroArmi()
    {
        var s = new Nemico()
        {
            Nome = "Maestro",
            Descrizione = "Maestro dell'arte della spada.",
            Salute = 160,
            SaluteMax = 160,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Taglio",
                    Danno = 15,
                    PesoProbabilita = 2,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan, nem);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Disarmo",
                    PesoProbabilita = 1,
                    EffettiSpeciali = (gioc, nem) =>
                    {
                        StatusEffect.Indebolimento(Target.Giocatore);
                    }
                }
            }
        };
        s.Pesa();
        return s;
    }

    /// <summary>
    /// Crea un'istanza preconfigurata del Guardiano della Cripta (miniboss).
    /// </summary>
    /// <returns>Un nuovo Guardiano della Cripta pronto per il combattimento.</returns>
    public static Nemico Guardiano()
    {
        var s = new Nemico()
        {
            Nome = "Guardiano Della Cripta",
            Descrizione = "Maestro dell'arte dello scudo.",
            Salute = 185,
            SaluteMax = 185,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Colpo di Scudo",
                    Danno = 10,
                    PesoProbabilita = 2,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan, nem);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Colpo in Testa",
                    PesoProbabilita = 1,
                    EffettiSpeciali = (gioc, nem) =>
                    {
                        StatusEffect.Indebolimento(Target.Giocatore);
                    }
                }
            }
        };
        s.Pesa();
        return s;
    }

    /// <summary>
    /// Crea un'istanza preconfigurata del Signore del Dungeon (boss finale).
    /// </summary>
    /// <returns>Un nuovo boss finale pronto per il combattimento.</returns>
    public static Nemico Boss()
    {
        var s = new Nemico()
        {
            Nome = "Signore del Dungeon",
            Descrizione = "Il signore oscuro del dungeon. Un essere di pura malvagità.",
            Salute = 300,
            SaluteMax = 300,
            Abilita = new()
            {
                new AbilitaNemico
                {
                    Nome = "Oscurità",
                    Danno = 20,
                    PesoProbabilita = 2,
                    AttaccaGiocatore = (gioc, nem, dan) =>
                    {
                        gioc.Danneggia(dan, nem);
                    }
                },
                new AbilitaNemico
                {
                    Nome = "Maledizione",
                    PesoProbabilita = 1,
                    EffettiSpeciali = (gioc, nem) =>
                    {
                        StatusEffect.Indebolimento(Target.Giocatore);
                    }
                }
            }
        };
        s.Pesa();
        return s;
    }

    /// <summary>
    /// Calcola il peso totale delle abilità sommando i singoli <see cref="AbilitaNemico.PesoProbabilita"/>.
    /// Deve essere chiamato dopo aver configurato la lista <see cref="Abilita"/>.
    /// </summary>
    private void Pesa()
    {
        int tot = 0;
        foreach(var a in Abilita)
        {
            tot += a.PesoProbabilita;
        }
        totPeso = tot;
    }

    /// <summary>
    /// Seleziona casualmente un'abilità da usare, pesata in base a <see cref="AbilitaNemico.PesoProbabilita"/>.
    /// </summary>
    /// <returns>L'abilità selezionata.</returns>
    /// <exception cref="Exception">Se nessuna abilità viene selezionata (non dovrebbe accadere).</exception>
    public AbilitaNemico ScegliAbilita()
    {
        var rand = new Random();
        int risultato = rand.Next(totPeso +1);
        foreach(var a in Abilita)
        {
            risultato -= a.PesoProbabilita;
            if(risultato<=0) return a;
        }
        throw new Exception();
    }
}

/// <summary>
/// Rappresenta un'abilità utilizzabile da un <see cref="Nemico"/> in combattimento.
/// Supporta condizioni speciali, danni diretti ed effetti collaterali.
/// </summary>
public class AbilitaNemico
{
    /// <summary>Nome dell'abilità.</summary>
    public string Nome = "";
    /// <summary>Danno base inflitto dall'abilità.</summary>
    public int Danno = 0;
    /// <summary>Peso per la selezione probabilistica (più alto = più probabile).</summary>
    public int PesoProbabilita;

    /// <summary>
    /// Condizione che deve essere soddisfatta per poter usare l'abilità.
    /// Prende giocatore e nemico come parametri, restituisce <c>true</c> se l'abilità è utilizzabile.
    /// Non serializzabile: contiene delegati.
    /// </summary>
    [JsonIgnore]
    public Func<Giocatore, Nemico, bool>? CondizioneSpeciale;
    /// <summary>
    /// Azione che infligge danni al giocatore. Il terzo parametro è il danno da applicare.
    /// Non serializzabile: contiene delegati.
    /// </summary>
    [JsonIgnore]
    public Action<Giocatore, Nemico, int>? AttaccaGiocatore;
    /// <summary>
    /// Effetti speciali aggiuntivi (es. status effect) da applicare oltre al danno.
    /// Non serializzabile: contiene delegati.
    /// </summary>
    [JsonIgnore]
    public Action<Giocatore, Nemico>? EffettiSpeciali;

    /// <summary>
    /// Esegue l'abilità: verifica le condizioni, infligge danni e applica effetti speciali.
    /// </summary>
    /// <param name="nemico">Il nemico che sta usando l'abilità.</param>
    /// <param name="giocatore">Il giocatore bersaglio.</param>
    public void Esegui(Nemico nemico, Giocatore giocatore)
    {
        if(CondizioneSpeciale != null && CondizioneSpeciale(giocatore, nemico) == false)
        {
            Logger.Get<AbilitaNemico>().LogDebug("Condizione non soddisfatta per {Abilita} di {Nemico}", Nome, nemico.Nome);
            UI.MostraErrore($"Il Nemico non riesce ad usare la abilità {Nome}!!");
            return;
        }
        Logger.Get<AbilitaNemico>().LogDebug("{Nemico} usa {Abilita} (danno: {Danno})", nemico.Nome, Nome, Danno);
        AttaccaGiocatore?.Invoke(giocatore, nemico, Danno);
        EffettiSpeciali?.Invoke(giocatore, nemico);
    }
}
