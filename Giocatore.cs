using System;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

/// <summary>
/// Interfaccia per entità che possono ricevere danni o cure.
/// </summary>
public interface IDannegiabile
{
    /// <summary>Applica una quantità di danni all'entità.</summary>
    /// <param name="d">Quantità di danni da infliggere.</param>
    void Danneggia(int d, IDannegiabile? avversario);
    /// <summary>Ripristina una quantità di punti vita all'entità.</summary>
    /// <param name="c">Quantità di cure da applicare.</param>
    void Cura(int c);
}

/// <summary>
/// Rappresenta il personaggio giocante. Gestisce punti vita, stamina, inventario,
/// equipaggiamento, chiavi, status effect, oro e le azioni di combattimento.
/// </summary>
public class Giocatore : IDannegiabile
{
    /// <summary>Nome del personaggio.</summary>
    public string Nome {get; set;}  = "";

    /// <summary>Punti vita attuali.</summary>
    public int PuntiVita {get; set;}
    /// <summary>Punti vita massimi.</summary>
    public int PuntiVitaMax {get; set;}

    /// <summary>Stamina attuale.</summary>
    public int Stamina {get; set;}
    /// <summary>Stamina massima.</summary>
    public int StaminaMax {get; set;}

    /// <summary>Valore di difesa che riduce i danni subiti.</summary>
    public int Difesa = 0;

    /// <summary>Quantità di oro posseduta.</summary>
    public int Oro {get; set;}

    /// <summary>Inventario degli oggetti trasportati (stack LIFO).</summary>
    [JsonInclude]
    [JsonPropertyName("Inventario")]
    private Stack<Oggetto> _inventario = new();
    [JsonIgnore]
    public Stack<Oggetto> Inventario => _inventario;
    /// <summary>Capacità massima dell'inventario (in unità di peso).</summary>
    [JsonInclude]
    [JsonPropertyName("InventarioMax")]
    private int _inventarioMax = 10;
    [JsonIgnore]
    public int InventarioMax => _inventarioMax;

    /// <summary>Arma attualmente equipaggiata, o <c>null</c>.</summary>
    [JsonInclude]
    [JsonPropertyName("Arma")]
    private Armi? _arma;
    [JsonIgnore]
    public Armi? Arma => _arma;

    /// <summary>Modificatore da applicare al danno in uscita (può essere negativo).</summary>
    public int ModificatoreDanno { get; set; }

    /// <summary>Equipaggia un'arma, sostituendo quella attuale.</summary>
    /// <param name="arma">L'arma da equipaggiare.</param>
    public void EquipaggiaArma(Armi arma) => _arma = arma;

    /// <summary>
    /// Ricostruisce l'<see cref="Armi.AbilitaArma"/> dell'arma equipaggiata in base al nome.
    /// Necessario dopo il caricamento di un salvataggio, poiché le abilità non vengono serializzate.
    /// </summary>
    public void RicostruisciAbilitaArma()
    {
        if (_arma is null) return;
        _arma.AbilitaArma = _arma.Nome switch
        {
            string n when n.StartsWith("Spada", StringComparison.InvariantCultureIgnoreCase) => new ColpoPotente(),
            string n when n.StartsWith("Scudo", StringComparison.InvariantCultureIgnoreCase) => new RiflettiScudo(),
            string n when n.StartsWith("Coltello", StringComparison.InvariantCultureIgnoreCase) => new Sanguinamento(),
            _ => null
        };
    }

    [JsonInclude]
    private HashSet<string> _chiavi = new();

    /// <summary>Insieme degli ID delle chiavi possedute dal giocatore.</summary>
    [JsonIgnore]
    public HashSet<string> Chiavi => _chiavi;

    /// <summary>Verifica se il giocatore possiede una chiave con l'ID specificato.</summary>
    /// <param name="id">ID della chiave da cercare.</param>
    /// <returns><c>true</c> se la chiave è posseduta.</returns>
    public bool HaChiave(string id) => _chiavi.Contains(id);
    /// <summary>Aggiunge una chiave all'inventario chiavi.</summary>
    /// <param name="id">ID della chiave da aggiungere.</param>
    public void DaiChiave(string id) => _chiavi.Add(id);

    /// <summary>
    /// Raccoglie un oggetto dalla stanza: se è un'arma la equipaggia,
    /// se è una chiave la aggiunge all'inventario chiavi, altrimenti lo mette nell'inventario.
    /// </summary>
    /// <param name="o">Oggetto da raccogliere.</param>
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
        if (!AggiungiOggettoInventario(o))
            return;
        Logger.Get<Giocatore>().LogInformation("Oggetto raccolto: {Oggetto}", o.Nome);
        UI.MostraMessaggio($"Hai raccolto: {o.Nome}.");
    }

    [JsonInclude]
    private List<StatusEffect> _statusEffects = new();

    /// <summary>Lista degli status effect attualmente attivi sul giocatore.</summary>
    [JsonIgnore]
    public List<StatusEffect> StatusEffects => _statusEffects;

    /// <summary>
    /// Costruttore usato per la deserializzazione JSON.
    /// I nomi dei parametri devono corrispondere (case-insensitive) a proprietà o campi della classe.
    /// </summary>
    /// <param name="nome">Nome del personaggio (match con <see cref="Nome"/>).</param>
    /// <param name="puntiVitaMax">Punti vita massimi (match con <see cref="PuntiVitaMax"/>).</param>
    /// <param name="staminaMax">Stamina massima (match con <see cref="StaminaMax"/>).</param>
    [JsonConstructor]
    public Giocatore(string nome, int puntiVitaMax = 100, int staminaMax = 20)
    {
        Nome = nome;
        PuntiVita = puntiVitaMax;
        PuntiVitaMax = puntiVitaMax;
        Stamina = staminaMax;
        StaminaMax = staminaMax;
        Oro = 0;
    }

    /// <summary>Imposta il nome del personaggio.</summary>
    /// <param name="s">Nuovo nome.</param>
    public void InserimentoNome(string s)
    {
        Nome = s;
    }

    /// <summary>
    /// Aggiunge (o sottrae) una quantità di oro. Se il totale scende sotto 0, viene azzerato.
    /// </summary>
    /// <param name="valore">Quantità di oro da aggiungere (può essere negativo).</param>
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
    }

    /// <summary>
    /// Applica un danno al giocatore, riducendolo in base alla difesa.
    /// Se i punti vita scendono sotto 0, viene chiamato il Game Over.
    /// </summary>
    /// <param name="danno">Danno base da infliggere.</param>
    public void Danneggia(int danno, IDannegiabile? attaccante = null)
    {
        if(attaccante is Nemico){
            danno = StatusEffect.ProcessaDanno(_statusEffects, this, danno, (Nemico)attaccante);
        }
        else
        {
            danno = StatusEffect.ProcessaDanno(_statusEffects, this, danno, null);
        }
        int dannoEffettivo = danno - Difesa;
        PuntiVita -= dannoEffettivo;
        Logger.Get<Giocatore>().LogDebug("Giocatore subisce {Danno} danni (difesa: {Difesa}, effettivi: {Effettivo}) (HP: {HP}/{Max})", danno, Difesa, dannoEffettivo, PuntiVita, PuntiVitaMax);
        UI.MostraDanno(GameManager.Giocatore.Nome, dannoEffettivo);
        if (PuntiVita <= 0)
        {
            Logger.Get<Giocatore>().LogInformation("GAME OVER: {Nome} è morto", Nome);
            UI.GameOver(this);
        }
    }

    /// <summary>
    /// Cura il giocatore di una quantità, senza superare i punti vita massimi.
    /// </summary>
    /// <param name="cura">Quantità di cure da applicare.</param>
    public void Cura(int cura)
    {
        PuntiVita += cura;
        if(PuntiVita > PuntiVitaMax)
        {
            PuntiVita = PuntiVitaMax;
        }
        Logger.Get<Giocatore>().LogDebug("Giocatore curato di {Cura} (HP: {HP}/{Max})", cura, PuntiVita, PuntiVitaMax);
    }

    /// <summary>
    /// Modifica la stamina del giocatore. Se il costo supera la stamina disponibile,
    /// l'operazione fallisce. La stamina non può superare il massimo.
    /// </summary>
    /// <param name="quantita">Quantità da aggiungere (positiva) o sottrarre (negativa).</param>
    /// <returns><c>true</c> se la modifica è andata a buon fine, <c>false</c> se la stamina era insufficiente.</returns>
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

    /// <summary>
    /// Aggiunge un oggetto all'inventario. Per i consumabili, verifica che il peso totale
    /// non superi la capacità massima. Restituisce <c>false</c> se l'inventario è pieno.
    /// </summary>
    /// <param name="o">Oggetto da aggiungere all'inventario.</param>
    /// <returns><c>true</c> se l'oggetto è stato aggiunto, <c>false</c> se l'inventario è pieno.</returns>
    public bool AggiungiOggettoInventario(Oggetto o)
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
                UI.MostraMessaggio($"Inventario pieno! Peso totale: {spazio - consAggiunto.peso}/{InventarioMax}, richiesto: +{consAggiunto.peso}.");
                return false;
            }
        }
        Inventario.Push(o);
        return true;
    }

    /// <summary>
    /// Fa cadere un oggetto in una stanza, aggiungendolo agli oggetti a terra
    /// e creando un'azione "raccogli" corrispondente.
    /// </summary>
    /// <param name="s">Stanza in cui far cadere l'oggetto.</param>
    /// <param name="o">Oggetto da far cadere.</param>
    public static void FaiCadereOggetto(Stanza s, Oggetto o)
    {
        s.AggiungiOggettoRaccoglibile(o);
    }

    /// <summary>
    /// Rimuove e restituisce l'ultimo oggetto dall'inventario (stack LIFO).
    /// Se lasciato volontariamente, l'oggetto viene fatto cadere nella stanza corrente.
    /// </summary>
    /// <param name="lasciatoVolontariamente">Se <c>true</c>, l'oggetto viene lasciato nella stanza.</param>
    /// <returns>L'oggetto rimosso, o <c>null</c> se l'inventario è vuoto.</returns>
    public Oggetto? RimuoviOggettoInventario(bool lasciatoVolontariamente = true)
    {
        if (Inventario.Count() != 0)
        {
            var o = Inventario.Pop();
            if (lasciatoVolontariamente && GameManager.StatoGioco is EsplorazioneStanza esplorazione)
            {
                o.IdSalvataggio = "caduto_" + Guid.NewGuid().ToString();
                Stanza s = esplorazione._stanza;
                FaiCadereOggetto(s, o);
                UI.MostraMessaggio($"Hai lasciato cadere {o.Nome} nella stanza.");
            }
            return o;
        }
        return null;
    }

    /// <summary>
    /// Dizionario che mappa i nomi delle azioni di combattimento ai metodi statici corrispondenti.
    /// Non serializzabile: contiene delegati ricostruiti a runtime.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, Action<EsplorazioneStanza, Nemico>> AzioniCombattimento = new()
    {
        {
            "attacca", Attacca
        }
    };

    /// <summary>
    /// Esegue un attacco base contro il nemico, usando la potenza dell'arma equipaggiata
    /// più il modificatore di danno.
    /// </summary>
    /// <param name="contesto">Contesto di esplorazione corrente (non utilizzato direttamente).</param>
    /// <param name="nem">Nemico da attaccare.</param>
    public static void Attacca(EsplorazioneStanza contesto, Nemico nem)
    {
        int danno = (GameManager.Giocatore.Arma?.potenza ?? 0) + GameManager.Giocatore.ModificatoreDanno;
        if (danno < 0) danno = 0;
        Logger.Get<Giocatore>().LogDebug("Attacco a {Nemico} per {Danno} danni", nem.Nome, danno);
        nem.Danneggia(danno);
    }

    /// <summary>
    /// Usa l'abilità speciale dell'arma equipaggiata contro il nemico.
    /// Sottrae la stamina richiesta; se insufficiente o se non c'è un'arma equipaggiata, restituisce <c>false</c>.
    /// </summary>
    /// <param name="contesto">Contesto di esplorazione corrente (non utilizzato direttamente).</param>
    /// <param name="nem">Nemico bersaglio dell'abilità.</param>
    /// <returns><c>true</c> se l'abilità è stata eseguita, <c>false</c> se è fallita.</returns>
    public static bool UsaAbilitaArma(EsplorazioneStanza contesto, Nemico nem)
    {
        try{
            Armi armaEquipaggiata = GameManager.Giocatore.Arma ?? throw new NullReferenceException();
            if(armaEquipaggiata.AbilitaArma == null)
                throw new NullReferenceException();
            if (!GameManager.Giocatore.CambiaStamina(-armaEquipaggiata.stamina))
                return false;
            Logger.Get<Giocatore>().LogDebug("Abilità arma usata: {Abilita} su {Nemico}", armaEquipaggiata.AbilitaArma.Nome, nem.Nome);
            armaEquipaggiata.AbilitaArma.Esegui(GameManager.Giocatore, nem);
            return true;
        }
        catch (NullReferenceException)
        {
            Logger.Get<Giocatore>().LogWarning("Tentativo uso abilità senza arma equipaggiata");
            UI.MostraErrore("Nessuna arma.");
            return false;
        }
    }

    /// <summary>
    /// Usa il consumabile in cima all'inventario (stack LIFO).
    /// Restituisce <c>false</c> se l'inventario è vuoto o l'oggetto in cima non è un consumabile.
    /// </summary>
    /// <param name="contesto">Contesto di esplorazione corrente (non utilizzato direttamente).</param>
    /// <param name="nem">Nemico corrente (non utilizzato direttamente).</param>
    /// <returns><c>true</c> se il consumabile è stato usato, <c>false</c> altrimenti.</returns>
    public static bool UsaConsumabile(EsplorazioneStanza contesto, Nemico nem)
    {
        if (GameManager.Giocatore.Inventario.Count == 0)
        {
            UI.MostraMessaggio("Il tuo inventario è vuoto.");
            return false;
        }
        GameManager.Giocatore.Inventario.TryPeek(out var ogg);
        if(ogg is Consumabili)
        {
            Consumabili consumabile = (Consumabili)GameManager.Giocatore.Inventario.Pop();
            Logger.Get<Giocatore>().LogInformation("Consumabile usato: {Oggetto} (HP: {HP}, Stam: {Stam})", consumabile.Nome, GameManager.Giocatore.PuntiVita, GameManager.Giocatore.Stamina);
            consumabile.Usa();
            return true;
        }
        UI.MostraMessaggio("L'ultimo oggetto nell'inventario non è un consumabile.");
        return false;
    }
}

/// <summary>
/// Indica il bersaglio a cui è applicato uno status effect.
/// </summary>
public enum Target
{
    /// <summary>L'effetto è applicato al giocatore.</summary>
    Giocatore,
    /// <summary>L'effetto è applicato al nemico.</summary>
    Nemico
}

/// <summary>
/// Rappresenta uno status effect temporaneo (es. bruciatura, indebolimento, sbilanciamento).
/// Ha una durata in turni e callback opzionali all'inizio di ogni turno e alla rimozione.
/// </summary>
public class StatusEffect
{
    /// <summary>Nome descrittivo dell'effetto.</summary>
    public string Name = "";
    /// <summary>Turni rimanenti prima che l'effetto scada.</summary>
    public int turniRimanenti = 0;
    /// <summary>Bersaglio dell'effetto (giocatore o nemico).</summary>
    public Target target;
    /// <summary>Callback eseguito all'inizio di ogni turno mentre l'effetto è attivo. Non serializzabile.</summary>
    [JsonIgnore]
    public Action<object>? onTurnStart;
    /// <summary>Callback eseguito quando l'effetto viene rimosso. Non serializzabile.</summary>
    [JsonIgnore]
    public Action<object>? onRemove;
    /// <summary>Callback eseguito quando il proprietario prende danno. Non serializzabile.</summary>
    [JsonIgnore]
    public Func<object, int, Nemico?, int>? onDamaged;
    public static int ProcessaDanno(List<StatusEffect> effects, object target, int danno, Nemico? attaccante)
    {
        for (int i = effects.Count - 1; i >= 0; i--){
            if (effects[i].onDamaged != null)
                danno = effects[i].onDamaged!(target, danno, attaccante);
        }
        return danno;
    }

    /// <summary>
    /// Processa tutti gli status effect di una lista: esegue <see cref="onTurnStart"/>,
    /// decrementa i turni rimanenti e, se scaduti, esegue <see cref="onRemove"/> e li rimuove.
    /// </summary>
    /// <param name="effects">Lista di effetti da processare.</param>
    /// <param name="target">Entità bersaglio a cui sono applicati gli effetti.</param>
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

    /// <summary>
    /// Crea un effetto di bruciatura che infligge 1 danno a turno al giocatore.
    /// </summary>
    /// <param name="target">Bersaglio dell'effetto.</param>
    /// <param name="durata">Durata in turni (default: 3).</param>
    /// <returns>L'effetto di bruciatura creato.</returns>
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

    /// <summary>
    /// Crea un effetto di indebolimento che riduce il modificatore di danno del giocatore di 10.
    /// </summary>
    /// <param name="target">Bersaglio dell'effetto.</param>
    /// <param name="durata">Durata in turni (default: 3).</param>
    /// <returns>L'effetto di indebolimento creato.</returns>
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

    /// <summary>
    /// Crea un effetto di sbilanciamento che ha una probabilità del 10% a turno
    /// di ridurre la difesa del giocatore.
    /// </summary>
    /// <param name="target">Bersaglio dell'effetto.</param>
    /// <param name="durata">Durata in turni (default: 3).</param>
    /// <returns>L'effetto di sbilanciamento creato.</returns>
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

    /// <summary>
    /// Crea un effetto che riduce temporaneamente la difesa del giocatore.
    /// </summary>
    /// <param name="target">Bersaglio dell'effetto.</param>
    /// <param name="durata">Durata in turni (default: 1).</param>
    /// <param name="quantita">Quantità di difesa da ridurre (default: 5).</param>
    /// <returns>L'effetto di riduzione difesa creato.</returns>
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


/// <summary>
/// Gestisce la serializzazione e deserializzazione dello stato di gioco in formato JSON.
/// Salva giocatore, stato delle porte, oggetti a terra, nemici sconfitti e assegnazioni miniboss.
/// </summary>
public static class JsonSalvataggio
{
    /// <summary>Percorso del file di salvataggio.</summary>
    const string percorso = "salvataggio.json";
    /// <summary>Opzioni di serializzazione JSON con indentazione e supporto enum come stringhe.</summary>
    private static readonly JsonSerializerOptions Opzioni = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Rappresenta lo stato di una singola porta in una stanza per il salvataggio.
    /// </summary>
    public class StatoPorteFlag
    {
        /// <summary>ID della stanza che contiene la porta.</summary>
        public string Stanza { get; set; } = "";
        /// <summary>Direzione della porta.</summary>
        public Direzione Direzione { get; set; }
        /// <summary>Stato della porta (Aperta, Chiusa, Bloccata).</summary>
        public StatoPorta Stato { get; set; }
    }

    /// <summary>
    /// Rappresenta un oggetto lasciato cadere a terra dal giocatore.
    /// </summary>
    public class OggettiCadutiFlag
    {
        /// <summary>ID della stanza in cui l'oggetto è caduto.</summary>
        public string IdStanza {get;set;} = "";
        /// <summary>L'oggetto caduto.</summary>
        public Oggetto Oggetto {get;set;} = null!;
    }

    /// <summary>
    /// Contiene tutti i flag necessari per ripristinare lo stato del mondo al caricamento.
    /// </summary>
    public class StatoMondoFlags
    {
        /// <summary>ID della stanza in cui si trova il giocatore.</summary>
        public string StanzaCorrenteId{ get; set; } = "";
        /// <summary>Lista degli stati di tutte le porte.</summary>
        public List<StatoPorteFlag> StatoPorte{ get; set; } = new();
        /// <summary>Lista degli ID salvataggio degli oggetti rimossi (raccolti) dalle stanze.</summary>
        public List<string> OggettiRimossi{ get; set; } = new();
        /// <summary>Lista degli ID salvataggio degli oggetti rimasti dal mercante.</summary>
        public List<string> OggettiMercante {get; set;} = new();
        /// <summary>Lista degli oggetti lasciati cadere a terra dal giocatore.</summary>
        public List<OggettiCadutiFlag> OggettiCaduti {get; set;} = new();
        /// <summary>Lista degli ID delle stanze i cui nemici sono stati sconfitti.</summary>
        public List<string> NemiciRimossi {get;set;} = new();
        /// <summary>Lista degli ID delle stanze in cui l'oro è già stato raccolto.</summary>
        public List<string> StanzeOroRaccolto {get;set;} = new();
        /// <summary>Lista degli ID delle stanze curative già usate.</summary>
        public List<string> StanzeCurativaUsata {get;set;} = new();
        /// <summary>Dizionario che associa ID stanza al nome del miniboss assegnato.</summary>
        public Dictionary<string, string> MinibossAssegnati {get; set;} = new();
    }

    /// <summary>
    /// Contenitore principale del salvataggio: giocatore + stato del mondo.
    /// </summary>
    public class Salvataggio
    {
        /// <summary>Dati del giocatore.</summary>
        public Giocatore Giocatore { get; set; } = null!;
        /// <summary>Flag dello stato del mondo.</summary>
        public StatoMondoFlags Mondo { get; set; } = new();
    }

    /// <summary>
    /// Cattura lo stato attuale del mondo in una struttura serializzabile.
    /// Include: stanza corrente, stati porte, nemici sconfitti, oggetti a terra, assegnazioni miniboss.
    /// </summary>
    /// <returns>Lo stato del mondo catturato.</returns>
    public static StatoMondoFlags CatturaMondo()
    {
        var dto = new StatoMondoFlags
        {
            StanzaCorrenteId = GameManager.StanzaCorrente.Id
        };
        foreach (var (id, nome) in Mappa.AssegnazioniMiniboss)
            dto.MinibossAssegnati[id] = nome;

        foreach (var s in Mappa.Stanze.Values)
        {
            foreach (var (dir, porta) in s.Porte)
            {
                dto.StatoPorte.Add(new StatoPorteFlag
                {
                    Stanza = s.Id,
                    Direzione = dir,
                    Stato = porta.Stato
                });
            }

            if (s.NemicoSconfitto)
                dto.NemiciRimossi.Add(s.Id);

            if (s.OroRaccolto)
                dto.StanzeOroRaccolto.Add(s.Id);

            if (s.CurativaUsata)
                dto.StanzeCurativaUsata.Add(s.Id);

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

    /// <summary>
    /// Applica lo stato del mondo caricato da un salvataggio: reinizializza la mappa,
    /// ripristina miniboss, nemici sconfitti, porte, oggetti e posizione del giocatore.
    /// </summary>
    /// <param name="dto">Dati dello stato mondo da applicare.</param>
    public static void ApplicaMondo(StatoMondoFlags dto)
    {
        Mappa.Inizializza();

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

        foreach (var idStanza in dto.NemiciRimossi)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == idStanza);
            if (stanza is not null)
            {
                stanza.NemicoSconfitto = true;
                stanza.NemicoStanza = null;
            }
        }

        foreach (var idStanza in dto.StanzeOroRaccolto)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == idStanza);
            if (stanza is not null)
            {
                stanza.OroRaccolto = true;
                stanza.RimuoviAzione("raccogli");
            }
        }

        foreach (var idStanza in dto.StanzeCurativaUsata)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == idStanza);
            if (stanza is not null)
            {
                stanza.CurativaUsata = true;
                if (stanza.Azioni.TryGetValue("riposati", out var azione))
                    azione.Descrizione = "La luce si è affievolita. Non puoi più riposarti qui.";
            }
        }

        foreach (var p in dto.StatoPorte)
        {
            var stanza = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == p.Stanza);
            if (stanza is null) continue;
            if (stanza.Porte.TryGetValue(p.Direzione, out var porta))
                porta.Stato = p.Stato;
        }

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
                stanza.OggettiStanza.Add(new OggettoTrovabile { oggetto = caduto.Oggetto, IsTrovabile = true });
        }
        foreach (var s in Mappa.Stanze.Values)
            s.RipristinaAzioniRaccogli();
        var corrente = Mappa.Stanze.Values.FirstOrDefault(s => s.Id == dto.StanzaCorrenteId);
        if (corrente is not null)
        {
            GameManager.StanzaCorrente = corrente;
            GameManager.CambiaStato(new EsplorazioneStanza(corrente));
        }
    }

    /// <summary>
    /// Salva lo stato corrente del gioco su file JSON.
    /// Rimuove preventivamente gli status effect per evitare corruzione delle statistiche.
    /// </summary>
    /// <param name="g">Giocatore da salvare.</param>
    public static void salva(Giocatore g)
    {
        RimuoviTuttiStatusEffects(g);
        var data = new Salvataggio { Giocatore = g, Mondo = CatturaMondo() };
        File.WriteAllText(percorso, JsonSerializer.Serialize(data, Opzioni));
        Logger.For("JsonSalvataggio").LogInformation("Partita salvata su {File}", percorso);
    }

    /// <summary>
    /// Rimuove tutti gli status effect attivi dal giocatore, chiamando onRemove per annullare
    /// le modifiche permanenti (es. Indebolimento ripristina ModificatoreDanno, DifesaGiu ripristina Difesa).
    /// Necessario prima del salvataggio per evitare corruzione delle statistiche.
    /// </summary>
    /// <param name="g">Giocatore da pulire.</param>
    private static void RimuoviTuttiStatusEffects(Giocatore g)
    {
        for (int i = g.StatusEffects.Count - 1; i >= 0; i--)
        {
            g.StatusEffects[i].onRemove?.Invoke(g);
        }
        g.StatusEffects.Clear();
    }

    /// <summary>
    /// Carica un salvataggio da file JSON.
    /// </summary>
    /// <returns>Il salvataggio caricato, o <c>null</c> se il file non esiste o è corrotto.</returns>
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
