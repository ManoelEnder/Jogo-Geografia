using System;
using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    public enum StructureKind { ControlRoom, Turbine, Substation, Automation, CoolingTower, SecondReactor, RegionalGrid }

    public enum IconKind
    {
        Energy, ControlPanel, Turbine, Substation, Automation, CoolingTower, Reactor, Grid,
        Heat, University, Wrench, Protest, Hearing, Press, Nucleo, Tap, Passive, Discount, Sound, Reputation, Settings, Lock
    }

    public enum EventTone { Positive, Negative, Decision }

    public enum EffectKind { None, PassiveMultiplier, ClickMultiplier, ReputationTemp }

    public enum PermanentKind { ClickBonus, PassiveBonus, EarlyDiscount }

    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public IconKind icon;
        public StructureKind structure;
        [Tooltip("Custo do nível 1.")] public double baseCost = 10;
        [Tooltip("custo = ceil(custoBase × crescimento^nível)")] public float costGrowth = 1.2f;
        [Tooltip("Energia por toque somada a cada nível.")] public double clickGain;
        [Tooltip("Energia por segundo somada a cada nível.")] public double passiveGain;
        [Tooltip("Energia produzida na campanha necessária para liberar a compra.")] public double unlockAtProduced;
        [Tooltip("Níveis em que a estrutura ganha uma nova etapa visual.")] public int[] visualMilestones = { 1, 10, 25 };
    }

    [Serializable]
    public class EventChoice
    {
        public string label;
        [TextArea(2, 4)] public string description;
        [Tooltip("Custo em segundos da produção de referência (passiva + toques estimados).")] public float costProductionSeconds;
        [Tooltip("Custo mínimo em Energia.")] public double costMin;
        [Tooltip("Recompensa imediata em segundos da produção de referência.")] public float rewardProductionSeconds;
        public double rewardMin;
        [Tooltip("Mudança permanente de reputação.")] public int reputationDelta;
        public EffectKind effect;
        [Tooltip("Multiplicador (ex.: 1,5 = +50%; 0,6 = -40%) ou pontos de reputação no caso de ReputationTemp.")] public float effectValue = 1f;
        [Tooltip("Duração do efeito em segundos de jogo.")] public float effectDuration;
        [Tooltip("Escolha aplicada se o evento ficar sem resposta até expirar.")] public bool isDefaultIfIgnored;
    }

    [Serializable]
    public class EventDef
    {
        public string id;
        public string title;
        [Tooltip("Manchete curta exibida no topo.")] public string headline;
        [TextArea(3, 8)] public string body;
        public IconKind icon;
        public EventTone tone;
        [Range(0, 100)] public int minReputation = 0;
        [Range(0, 100)] public int maxReputation = 100;
        public float weight = 1f;
        public List<EventChoice> choices = new List<EventChoice>();
    }

    [Serializable]
    public class PermanentUpgradeDef
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public IconKind icon;
        public PermanentKind kind;
        public int baseCost = 1;
        [Tooltip("Núcleos a mais por nível já comprado.")] public int costStep = 1;
        [Tooltip("Bônus por nível (1 = +100%; desconto 0,25 = -25%).")] public float valuePerLevel = 1f;
        public int maxLevel = 20;
        [Tooltip("Somente para desconto: quantas melhorias da lista recebem o desconto.")] public int affectsFirstUpgrades = 4;
    }

    /// <summary>
    /// Todos os números de balanceamento ficam aqui e podem ser editados no Inspector
    /// (Assets/NucleoVivo/Config/NucleoVivoConfig.asset).
    /// </summary>
    [CreateAssetMenu(fileName = "NucleoVivoConfig", menuName = "Núcleo Vivo/Configuração do jogo")]
    public class GameConfig : ScriptableObject
    {
        [Header("Economia")]
        public double startClickValue = 1;
        [Tooltip("Toques por segundo usados para estimar custos e recompensas dos eventos.")] public float referenceTapsPerSecond = 2f;
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();

        [Header("Eventos")]
        [Range(0, 100)] public float startReputation = 50;
        public float eventIntervalMin = 60f;
        public float eventIntervalMax = 120f;
        [Tooltip("Segundos até um evento pendente ser resolvido com a escolha padrão.")] public float pendingEventExpire = 180f;
        public List<EventDef> events = new List<EventDef>();

        [Header("Renascimento")]
        public double rebirthThreshold = 25000;
        public List<PermanentUpgradeDef> permanentUpgrades = new List<PermanentUpgradeDef>();

        [Header("Salvamento")]
        public float autosaveInterval = 10f;

        [Header("Renderização")]
        [Tooltip("Material dos sprites. Vazio = material padrão do SpriteRenderer.")] public Material spriteMaterial;

        void Reset() { ResetToDefaults(); }

        [ContextMenu("Restaurar valores padrão")]
        public void ResetToDefaults()
        {
            startClickValue = 1;
            referenceTapsPerSecond = 2f;
            startReputation = 50;
            eventIntervalMin = 60;
            eventIntervalMax = 120;
            pendingEventExpire = 180;
            rebirthThreshold = 25000;
            autosaveInterval = 10;

            // Valores ajustados por simulação: ~11 min até o 1º Renascimento tocando 2x/s,
            // ~7 min tocando 4x/s. Veja o README da pasta NucleoVivo.
            upgrades = new List<UpgradeDef>
            {
                Up("painel", "Painel de controle", "Operadores ajustam as barras de controle com mais precisão.", IconKind.ControlPanel, StructureKind.ControlRoom, 10, 2.2f, 1, 0, 0),
                Up("turbina", "Turbina auxiliar", "Uma turbina extra aproveita o vapor que sobrava no circuito.", IconKind.Turbine, StructureKind.Turbine, 50, 1.28f, 0, 1, 10),
                Up("subestacao", "Subestação", "Transformadores levam a energia até a rede de Nova Aurora.", IconKind.Substation, StructureKind.Substation, 700, 1.28f, 0, 4, 400),
                Up("automacao", "Sala de automação", "Sensores e monitores otimizam cada acionamento do reator.", IconKind.Automation, StructureKind.Automation, 3200, 2.2f, 5, 0, 2000),
                Up("torre", "Torre de resfriamento", "Mais resfriamento deixa o reator operar perto da potência máxima.", IconKind.CoolingTower, StructureKind.CoolingTower, 7000, 1.28f, 0, 15, 5000),
                Up("reator2", "Segundo reator", "Uma nova ala com reator próprio amplia a capacidade da usina.", IconKind.Reactor, StructureKind.SecondReactor, 24000, 1.3f, 0, 50, 15000),
                Up("rede", "Rede regional", "Linhas de alta tensão levam energia a bairros e municípios vizinhos.", IconKind.Grid, StructureKind.RegionalGrid, 70000, 1.3f, 0, 150, 40000),
            };

            events = new List<EventDef>
            {
                new EventDef
                {
                    id = "onda_calor", title = "Onda de calor em Nova Aurora", headline = "Termômetros passam de 39 °C e Nova Aurora pede mais energia",
                    body = "Hospitais e o metrô de Nova Aurora pedem reforço. A Secretaria de Infraestrutura promete pagar bem pela energia extra, mas o engenheiro de turno Caio Freitas teme desgastar as turbinas.",
                    icon = IconKind.Heat, tone = EventTone.Decision, weight = 1.2f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Atender ao pico", description = "Contrato emergencial e gratidão da cidade, mas as turbinas trabalham no limite.", rewardProductionSeconds = 60, rewardMin = 40, reputationDelta = 6, effect = EffectKind.PassiveMultiplier, effectValue = 0.75f, effectDuration = 45 },
                        new EventChoice { label = "Manter a produção estável", description = "A equipe técnica fica tranquila; alguns bairros reclamam de quedas de luz.", reputationDelta = -3, isDefaultIfIgnored = true },
                    }
                },
                new EventDef
                {
                    id = "convenio", title = "Convênio com a Universidade do Vale do Itaré", headline = "Universidade do Vale do Itaré propõe parceria com a usina",
                    body = "A professora Lúcia Navarro quer trazer estudantes de engenharia para estágios. Em troca, o laboratório dela recalibra os modelos de controle das turbinas.",
                    icon = IconKind.University, tone = EventTone.Positive, minReputation = 25, weight = 1f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Assinar o convênio", description = "Os modelos novos aumentam a produção passiva.", reputationDelta = 3, effect = EffectKind.PassiveMultiplier, effectValue = 1.5f, effectDuration = 120, isDefaultIfIgnored = true },
                        new EventChoice { label = "Pedir apoio à sala de controle", description = "Os estudantes ajudam os operadores a cada acionamento.", effect = EffectKind.ClickMultiplier, effectValue = 2f, effectDuration = 60 },
                    }
                },
                new EventDef
                {
                    id = "falha_turbina", title = "Falha em uma turbina auxiliar", headline = "Vibração anormal para uma turbina auxiliar",
                    body = "O mecânico-chefe Osvaldo Teixeira encontrou um mancal desgastado. Dá para trocar a peça agora, mas o fornecedor de Ribeira Alta cobra caro pela entrega urgente.",
                    icon = IconKind.Wrench, tone = EventTone.Negative, weight = 1f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Pagar o reparo imediato", description = "A peça chega hoje e a produção não cai.", costProductionSeconds = 45, costMin = 40 },
                        new EventChoice { label = "Consertar com a equipe", description = "Sem custo, mas a produção passiva cai enquanto a turbina está parada.", effect = EffectKind.PassiveMultiplier, effectValue = 0.6f, effectDuration = 120, isDefaultIfIgnored = true },
                    }
                },
                new EventDef
                {
                    id = "ato_ambiental", title = "Ato do Coletivo Rio Vivo", headline = "Coletivo Rio Vivo protesta na entrada da usina",
                    body = "Pescadores do Ribeirão Claro temem que a água devolvida mais quente afaste os peixes. A líder Dalva Monteiro pede dados abertos sobre temperatura e rejeitos.",
                    icon = IconKind.Protest, tone = EventTone.Decision, maxReputation = 80, weight = 1f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Contratar auditoria independente", description = "O Instituto Água Limpa publica os dados. A confiança cresce e abre portas para boas notícias.", costProductionSeconds = 60, costMin = 80, reputationDelta = 12 },
                        new EventChoice { label = "Manter a operação", description = "Sem gasto agora, mas a reputação cai até o assunto esfriar.", effect = EffectKind.ReputationTemp, effectValue = -12, effectDuration = 180, isDefaultIfIgnored = true },
                    }
                },
                new EventDef
                {
                    id = "audiencia", title = "Audiência pública em Pedra Branca", headline = "Moradores de Pedra Branca querem conhecer a expansão",
                    body = "A Associação de Moradores de Pedra Branca está dividida: comerciantes esperam empregos, e famílias da estrada vicinal temem o vaivém de caminhões das obras.",
                    icon = IconKind.Hearing, tone = EventTone.Decision, weight = 1f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Custear uma visita guiada", description = "Ônibus, lanche e uma tarde mostrando a sala de controle.", costProductionSeconds = 30, costMin = 40, reputationDelta = 8 },
                        new EventChoice { label = "Preservar o saldo", description = "A operação segue normalmente.", isDefaultIfIgnored = true },
                    }
                },
                new EventDef
                {
                    id = "imprensa", title = "Gazeta do Vale elogia a usina", headline = "Gazeta do Vale mostra como a usina mantém Nova Aurora acesa",
                    body = "A repórter Marina Okabe acompanhou um turno inteiro e mostrou hospitais e escolas funcionando graças à usina. Novos contratos chegam à diretoria.",
                    icon = IconKind.Press, tone = EventTone.Positive, minReputation = 65, weight = 1.2f,
                    choices = new List<EventChoice>
                    {
                        new EventChoice { label = "Dar entrevista exclusiva", description = "A demanda sobe e a produção passiva acompanha.", reputationDelta = 2, effect = EffectKind.PassiveMultiplier, effectValue = 1.75f, effectDuration = 120, isDefaultIfIgnored = true },
                        new EventChoice { label = "Abrir o turno da noite", description = "A equipe empolgada rende mais a cada toque.", effect = EffectKind.ClickMultiplier, effectValue = 2.5f, effectDuration = 75 },
                    }
                },
            };

            permanentUpgrades = new List<PermanentUpgradeDef>
            {
                new PermanentUpgradeDef { id = "maos", displayName = "Mãos experientes", description = "A equipe veterana dobra a Energia de cada toque.", icon = IconKind.Tap, kind = PermanentKind.ClickBonus, baseCost = 1, costStep = 1, valuePerLevel = 1f, maxLevel = 20 },
                new PermanentUpgradeDef { id = "heranca", displayName = "Herança térmica", description = "Projetos aprimorados aumentam toda a produção passiva.", icon = IconKind.Passive, kind = PermanentKind.PassiveBonus, baseCost = 1, costStep = 1, valuePerLevel = 0.5f, maxLevel = 20 },
                new PermanentUpgradeDef { id = "enxuta", displayName = "Engenharia enxuta", description = "Fornecedores conhecidos baratam as primeiras melhorias.", icon = IconKind.Discount, kind = PermanentKind.EarlyDiscount, baseCost = 1, costStep = 1, valuePerLevel = 0.25f, maxLevel = 3, affectsFirstUpgrades = 4 },
            };
        }

        static UpgradeDef Up(string id, string name, string desc, IconKind icon, StructureKind s, double cost, float growth, double click, double passive, double unlock)
        {
            return new UpgradeDef
            {
                id = id, displayName = name, description = desc, icon = icon, structure = s,
                baseCost = cost, costGrowth = growth, clickGain = click, passiveGain = passive, unlockAtProduced = unlock,
                visualMilestones = new[] { 1, 10, 25 }
            };
        }

        public static GameConfig CreateDefault()
        {
            var c = CreateInstance<GameConfig>();
            c.ResetToDefaults();
            return c;
        }

        public int UpgradeIndex(string id) => upgrades.FindIndex(u => u.id == id);
        public EventDef FindEvent(string id) => events.Find(e => e.id == id);
    }
}
