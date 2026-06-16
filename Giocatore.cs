using System;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

public interface IDannegiabile
{
    void Danneggia(int d);
    void Cura(int c);
}

public class Giocatore : IDannegiabile
{
    public string Nome {get; private set;}  = "";

    public int PuntiVita {get; set;}
    public int PuntiVitaMax {get; set;}

    public int Stamina {get; set;}
    public int StaminaMax {get; set;}

    public int Difesa = 0;

    public int Oro {get; set;}

    public Stack<Oggetto> Inventario {get; private set;} = new();
    public int InventarioMax {get; private set;} = 10;

    public Armi? Arma { get; private set; }

    public int ModificatoreDanno { get; set; }

    public void EquipaggiaArma(Armi arma) => Arma = arma;

    [JsonInclude]
    private HashSet<string> _chiavi = new();

    public HashSet<string> Chiavi => _chiavi;

    public bool HaChiave(string id) => _chiavi.Contains(id);
    public void DaiChiave(string id) => _chiavi.Add(id);

    public void Raccogli(Oggetto o)
    {
        if (o is Armi arma)
        {
            EquipaggiaArma(arma);
            Logger.Get<Giocatore>().LogInformation("Arma equipaggiata: {Arma}", arma.Nome);
            UI.MostraMessaggio($"Hai equipaggiato: {arma.Nome}.");
            return;
        }
        if (o is OggettoChiave chiave)
        {
            DaiChiave(chiave.Serratura);
            Logger.Get<Giocatore>().LogInformation("Chiave raccolta: {Chiave} (serratura: {Serratura})", chiave.Nome, chiave.Serratura);
            UI.MostraMessaggio($"Hai raccolto: {chiave.Nome} (apre serrature {chiave.Serratura}).");
            return;
        }
        AggiungiOggettoInventario(o);
        Logger.Get<Giocatore>().LogInformation("Oggetto raccolto: {Oggetto}", o.Nome);
        UI.MostraMessaggio($"Hai raccolto: {o.Nome}.");
    }

    [JsonInclude]
    private List<StatusEffect> _statusEffects = new();

    public List<StatusEffect> StatusEffects => _statusEffects;

    [JsonConstructor]
    public Giocatore(string nome, int pvMax = 20, int staminaMax = 10)
    {
        Nome = nome;
        PuntiVita = pvMax;
        PuntiVitaMax = pvMax;
        Stamina = staminaMax;
        StaminaMax = staminaMax;
        Oro = 0;
    }

    public void InserimentoNome(string s)
    {
        Nome = s;
    }

    public void AggiungiOro(int valore)
    {
        Oro += valore;
        if (Oro < 0)
        {
            Logger.Get<Giocatore>().LogDebug("Oro sceso sotto 0, azzerato");
            Console.WriteLine("Oro minore di 0\n");
            Oro = 0;
            return;
        }
        /*
        if(Oro > 255)
        {
            Oro = 255;
            return;
        }
        */
    }
    public void Danneggia(int danno)
    {
        int dannoEffettivo = danno - Difesa;
        PuntiVita -= dannoEffettivo;
        Logger.Get<Giocatore>().LogDebug("Giocatore subisce {Danno} danni (difesa: {Difesa}, effettivi: {Effettivo}) (HP: {HP}/{Max})", danno, Difesa, dannoEffettivo, PuntiVita, PuntiVitaMax);
        UI.MostraDanno(GameManager.Giocatore.Nome, dannoEffettivo);
        if (PuntiVita < 0)
        {
            Logger.Get<Giocatore>().LogInformation("GAME OVER: {Nome} è morto", Nome);
            UI.GameOver(this);
        }
    }
    public void Cura(int cura)
    {
        PuntiVita += cura;
        if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
        Logger.Get<Giocatore>().LogDebug("Giocatore curato di {Cura} (HP: {HP}/{Max})", cura, PuntiVita, PuntiVitaMax);
    }
    /*public void CambiaPV(int quantita, bool danno)
    {
        PuntiVita += quantita;
        if (PuntiVita < 0)
        {
            UI.GameOver(this);
        }else if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
    }*/
    public bool CambiaStamina(int quantita)
    {
        if(-quantita > Stamina)
        {
            UI.MostraMessaggio("Stamina Insufficente!\n");
            Logger.Get<Giocatore>().LogDebug("Stamina insufficiente (richiesta: {Richiesta}, disponibile: {Disponibile})", -quantita, Stamina);
            return false;
        }
        Stamina += quantita;
        if (Stamina > StaminaMax)
        {
            Stamina = StaminaMax;
        }
        Logger.Get<Giocatore>().LogDebug("Stamina cambiata di {Delta} (ora: {Attuale}/{Max})", quantita, Stamina, StaminaMax);
        return true;
    }

    public void AggiungiOggettoInventario(Oggetto o)
    {
        if (o is Consumabili consAggiunto)
        {
            int spazio = 0;
            foreach(var i in Inventario)
            {
                if(i is Consumabili consumabili)
                { 
                    spazio += consumabili.peso;
                }
            }
            spazio += consAggiunto.peso;
            if(spazio > InventarioMax)
            {
                //evento: oggetto non entra nel inventario
                return;
            }
        }
        Inventario.Push(o);
    }

    public static void FaiCadereOggetto(Stanza s, Oggetto o)
    {
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = o });
        string idAzione = $"raccogli {o.Nome.ToLower()}";
        if (!s.Azioni.ContainsKey(idAzione))
        {
            s.AggiungiAzione(
                idAzione,
                $"Raccogli {o.Nome}",
                $"Raccogli {o.Nome} da terra.",
                () => 
                {
                    var daRaccogliere = s.OggettiStanza.FirstOrDefault(x => x.oggetto.Nome == o.Nome);
                    if (daRaccogliere != null)
                    {
                        GameManager.Giocatore.Raccogli(daRaccogliere.oggetto);
                        s.OggettiStanza.Remove(daRaccogliere);
                    }
                    // Rimuovi l'azione dalla stanza solo se non ci sono più oggetti con quel nome
                    if (!s.OggettiStanza.Any(x => x.oggetto.Nome == o.Nome))
                    {
                        s.RimuoviAzione(idAzione);
                    }
                }
            );
        }    
    }

    public Oggetto? RimuoviOggettoInventario(bool lasciatoVolontariamente = true)
    {
        if (Inventario.Count() != 0)
        {
            var o = Inventario.Pop();
            if (lasciatoVolontariamente && GameManager.StatoGioco is EsplorazioneStanza esplorazione)
            {
                // Prefisso speciale per salvarlo su JSON
                o.IdSalvataggio = "caduto_" + Guid.NewGuid().ToString(); 
                Stanza s = esplorazione._stanza;
                FaiCadereOggetto(s, o);
                // s.OggettiStanza.Add(new OggettoTrovabile { oggetto = o });
                // string idAzione = $"raccogli {o.Nome.ToLower()}";
                // if (!s.Azioni.ContainsKey(idAzione))
                // {
                //     s.AggiungiAzione(
                //         idAzione,
                //         $"Raccogli {o.Nome}",
                //         $"Raccogli {o.Nome} da terra.",
                //         () => 
                //         {
                //             var daRaccogliere = s.OggettiStanza.FirstOrDefault(x => x.oggetto.Nome == o.Nome);
                //             if (daRaccogliere != null)
                //             {
                //                 GameManager.Giocatore.Raccogli(daRaccogliere.oggetto);
                //                 s.OggettiStanza.Remove(daRaccogliere);
                //             }
                //             // Rimuovi l'azione dalla stanza solo se non ci sono più oggetti con quel nome
                //             if (!s.OggettiStanza.Any(x => x.oggetto.Nome == o.Nome))
                //             {
                //                 s.RimuoviAzione(idAzione);
                //             }
                //         }
                //     );
                // }
                UI.MostraMessaggio($"Hai lasciato cadere {o.Nome} nella stanza.");
            }
            return o;
        }
        return null;
    }

    public Dictionary<string, Action<EsplorazioneStanza, Nemico>> AzioniCombattimento = new()
    {
        {
            "attacca", Attacca
        },
        { 
            "abilità", UsaAbilitaArma
        }
    };
    
    public static void Attacca(EsplorazioneStanza contesto, Nemico nem)
    {
        int danno = (GameManager.Giocatore.Arma?.potenza ?? 0) + GameManager.Giocatore.ModificatoreDanno;
        if (danno < 0) danno = 0;
        Logger.Get<Giocatore>().LogDebug("Attacco a {Nemico} per {Danno} danni", nem.Nome, danno);
        nem.Danneggia(danno);
    }

    public static void UsaAbilitaArma(EsplorazioneStanza contesto, Nemico nem)
    {
        try{
            Armi armaEquipaggiata = GameManager.Giocatore.Arma ?? throw new NullReferenceException();
            if(armaEquipaggiata.AbiitaArma != null)
            {
                Logger.Get<Giocatore>().LogDebug("Abilità arma usata: {Abilita} su {Nemico}", armaEquipaggiata.AbiitaArma.Nome, nem.Nome);
                armaEquipaggiata.AbiitaArma?.Esegui(GameManager.Giocatore, nem);
            }
        }
        catch (NullReferenceException)
        {
            Logger.Get<Giocatore>().LogWarning("Tentativo uso abilità senza arma equipaggiata");
            UI.MostraErrore("Nessuna arma.");
        }
    }
    public static void UsaConsumabile(EsplorazioneStanza contesto, Nemico nem)
    {
        GameManager.Giocatore.Inventario.TryPeek(out var ogg);
        if(ogg is Consumabili)
        {
            Consumabili consumabile = (Consumabili)GameManager.Giocatore.Inventario.Pop();
            Logger.Get<Giocatore>().LogInformation("Consumabile usato: {Oggetto} (HP: {HP}, Stam: {Stam})", consumabile.Nome, GameManager.Giocatore.PuntiVita, GameManager.Giocatore.Stamina);
            consumabile.Usa();
        };
    }
}

public enum Target
{
    Giocatore,
    Nemico
}

public class StatusEffect
{
    public string Name = "";
    public int turniRimanenti = 0;
    public Target target;
    public Action<object>? onTurnStart;
    public Action<object>? onRemove;

    public static void ProcessaTurno(List<StatusEffect> effects, object target)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var eff = effects[i];
            eff.onTurnStart?.Invoke(target);
            eff.turniRimanenti--;
            if (eff.turniRimanenti <= 0)
            {
                eff.onRemove?.Invoke(target);
                effects.RemoveAt(i);
            }
        }
    }

    public static StatusEffect Bruciatura(Target target, int durata = 3)
    {
        StatusEffect burn = new(){
            target = target,
            Name = "bruciatura",
            turniRimanenti = durata
        };
        if(target == Target.Giocatore)
        {
            GameManager.Giocatore.StatusEffects.Add(burn);
            burn.onTurnStart = (sender) => GameManager.Giocatore.Danneggia(1);
        }
        return burn;
    }

    public static StatusEffect Indebolimento(Target target, int durata = 3)
    {
        StatusEffect weak = new()
        {
            target = target,
            Name = "indebolimento",
            turniRimanenti = durata
        };
        if (target == Target.Giocatore)
        {
            GameManager.Giocatore.ModificatoreDanno -= 10;
            GameManager.Giocatore.StatusEffects.Add(weak);
            weak.onRemove = (sender) => GameManager.Giocatore.ModificatoreDanno += 10;
        }
        return weak;
    }
    
    public static StatusEffect Sbilancio(Target target, int durata = 3)
    {
        StatusEffect bal = new()
        {
            target = target,
            Name = "sbilanciamento",
            turniRimanenti = durata
        };
        if (target == Target.Giocatore)
        {
            GameManager.Giocatore.StatusEffects.Add(bal);
            bal.onTurnStart = (sender) => 
                {
                    if(new Random().Next(1,11) == 10)
                    {
                        DifesaGiu(Target.Giocatore, 1, 10);
                    }
            }; 
        }
        return bal;
    }
    public static StatusEffect DifesaGiu(Target target, int durata = 1, int quantita = 5)
    {
        StatusEffect difg = new()
        {
            target = target,
            Name = $"Difesa -{quantita}",
            turniRimanenti = durata
        };
        if(target == Target.Giocatore)
        {
            GameManager.Giocatore.Difesa -= quantita;
            GameManager.Giocatore.StatusEffects.Add(difg);
            difg.onRemove = (sender) => GameManager.Giocatore.Difesa += quantita;
        }
        return difg;
    }
}


public static class JsonSalvataggio
{
    const string percorso = "salvataggio.json";
    private static readonly JsonSerializerOptions Opzioni = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public class StatoPorteFlag
    {
        public string Stanza { get; set; } = "";
        public Direzione Direzione { get; set; }
        public StatoPorta Stato { get; set; }
    }

    public class OggettiCadutiFlag
    {
        public string IdStanza {get;set;} = "";
        public Oggetto Oggetto {get;set;} = null!; 
    }

    public class StatoMondoFlags
    {
        public string StanzaCorrenteId{ get; set; } = "";
        public List<StatoPorteFlag> StatoPorte{ get; set; } = new();
        public List<string> OggettiRimossi{ get; set; } = new();
        public List<string> OggettiMercante {get; set;} = new();
        public List<OggettiCadutiFlag> OggettiCaduti {get; set;} = new();
        public List<string> NemiciRimossi {get;set;} = new();
        public Dictionary<string, string> MinibossAssegnati {get; set;} = new();
    }

    public class Salvataggio
    {
        public Giocatore Giocatore { get; set; } = null!;
        public StatoMondoFlags Mondo { get; set; } = new();
    }

    public static StatoMondoFlags CatturaMondo()
    {
        var dto = new StatoMondoFlags
        {
            StanzaCorrenteId = GameManager.StanzaCorrente.Id
        };
        // Salva assegnazioni miniboss
        foreach (var (id, nome) in Mappa.AssegnazioniMiniboss)
            dto.MinibossAssegnati[id] = nome;

        foreach (var s in Mappa.Stanze.Values)
        {
            // Salva lo stato delle porte
            foreach (var (dir, porta) in s.Porte)
            {
                dto.StatoPorte.Add(new StatoPorteFlag
                {
                    Stanza = s.Id,
                    Direzione = dir,
                    Stato = porta.Stato
                });
            }

            // Salva nemici sconfitti
            if (s.NemicoSconfitto)
                dto.NemiciRimossi.Add(s.Id);

            // Salva gli oggetti attualmente a terra nelle stanze.
            // Usa IdStabile (stringa) perché il Guid cambia ad ogni Inizializza().
            // Se un oggetto non ha IdStabile (drop nemico runtime, ecc.) viene ignorato
            // e quindi al load non verrà ripristinato, come previsto.
            foreach (var oggst in s.OggettiStanza)
            {
                if (oggst.oggetto.IdSalvataggio.StartsWith("caduto_"))
                {
                    dto.OggettiCaduti.Add(new OggettiCadutiFlag{ IdStanza = s.Id, Oggetto = oggst.oggetto });
                }
                if (!string.IsNullOrEmpty(oggst.oggetto.IdSalvataggio))
                    dto.OggettiRimossi.Add(oggst.oggetto.IdSalvataggio);
            }
        }
        return dto;
    }

    public static void ApplicaMondo(StatoMondoFlags dto)
    {
        // Re-inizializza la mappa (stato default)
        Mappa.Inizializza();

        // Ripristina assegnazioni miniboss
        foreach (var (idStanza, nome) in dto.MinibossAssegnati)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == idStanza);
            if (stanza is not null)
            {
                var nemico = Mappa.NemicoCopiabile.DaString(nome);
                if (nemico is not null)
                    stanza.NemicoStanza = nemico;
            }
        }

        // Ripristina nemici sconfitti
        foreach (var idStanza in dto.NemiciRimossi)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == idStanza);
            if (stanza is not null)
            {
                stanza.NemicoSconfitto = true;
                stanza.NemicoStanza = null;
            }
        }

        // Applica stato delle porte
        foreach (var p in dto.StatoPorte)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == p.Stanza);
            if (stanza is null) continue;
            if (stanza.Porte.TryGetValue(p.Direzione, out var porta))
                porta.Stato = p.Stato;
        }

        // Rimuovi dalle stanze gli oggetti statici che NON erano presenti al salvataggio
        // (cioè quelli che il giocatore aveva raccolto prima di salvare).
        // Oggetti senza IdStabile (drop runtime) non sono considerati persistenti.
        var presentiAlSalvataggio = new HashSet<string>(
            dto.OggettiRimossi.Where(id => !string.IsNullOrEmpty(id))
        );
        foreach (var s in Mappa.Stanze.Values)
        {
            s.OggettiStanza.RemoveAll(ot =>
                !string.IsNullOrEmpty(ot.oggetto.IdSalvataggio) &&
                !presentiAlSalvataggio.Contains(ot.oggetto.IdSalvataggio)
            );
        }
        foreach (var caduto in dto.OggettiCaduti)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == caduto.IdStanza);
            if (stanza is not null)
            {
                stanza.OggettiStanza.Add(new OggettoTrovabile { oggetto = caduto.Oggetto, IsTrovabile = true });
                Giocatore.FaiCadereOggetto(stanza, caduto.Oggetto);
                // string nomeOgg = caduto.Oggetto.Nome;
                // string idAzione = $"raccogli {nomeOgg.ToLower()}";
                
                // if (!stanza.Azioni.ContainsKey(idAzione))
                // {
                //     stanza.AggiungiAzione(
                //         idAzione,
                //         $"Raccogli {nomeOgg}",
                //         $"Raccogli {nomeOgg} da terra.",
                //         () => 
                //         {
                //             var daRaccogliere = stanza.OggettiStanza.FirstOrDefault(x => x.oggetto.Nome == nomeOgg);
                //             if (daRaccogliere != null)
                //             {
                //                 GameManager.Giocatore.Raccogli(daRaccogliere.oggetto);
                //                 stanza.OggettiStanza.Remove(daRaccogliere);
                //             }
                //             if (!stanza.OggettiStanza.Any(x => x.oggetto.Nome == nomeOgg))
                //             {
                //                 stanza.RimuoviAzione(idAzione);
                //             }
                //         }
                //     );
                // }
            }
        }
        // Riposiziona il giocatore
        var corrente = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == dto.StanzaCorrenteId);
        if (corrente is not null)
        {
            GameManager.StanzaCorrente = corrente;
            GameManager.CambiaStato(new EsplorazioneStanza(corrente));
        }
    }

    public static void salva(Giocatore g)
    {
        var data = new Salvataggio { Giocatore = g, Mondo = CatturaMondo() };
        File.WriteAllText(percorso, JsonSerializer.Serialize(data, Opzioni));
        Logger.For("JsonSalvataggio").LogInformation("Partita salvata su {File}", percorso);
    }

    public static Salvataggio? caricaSalvataggio()
    {
        if (!File.Exists(percorso))
        {
            Logger.For("JsonSalvataggio").LogWarning("File salvataggio non trovato: {File}", percorso);
            UI.MostraErrore("File non trovato.");
            return null;
        }
        Logger.For("JsonSalvataggio").LogInformation("Caricamento salvataggio da {File}", percorso);
        return JsonSerializer.Deserialize<Salvataggio>(File.ReadAllText(percorso), Opzioni);
    }
}

