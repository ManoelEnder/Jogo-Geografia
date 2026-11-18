using NUnit.Framework;
using UnityEngine;

namespace NucleoVivo.Tests
{
    public class EconomyTests
    {
        GameConfig cfg;
        GameState state;
        Economy eco;
        EventDirector ev;
        RebirthSystem rb;

        [SetUp]
        public void SetUp()
        {
            cfg = GameConfig.CreateDefault();
            state = new GameState { reputation = cfg.startReputation };
            eco = new Economy(cfg, () => state);
            ev = new EventDirector(cfg, eco, () => state, 1234);
            rb = new RebirthSystem(cfg, () => state);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(cfg);

        [Test]
        public void StartsWithOnePerTapAndNoPassive()
        {
            Assert.AreEqual(1, eco.ClickValue);
            Assert.AreEqual(0, eco.PassivePerSecond);
        }

        [Test]
        public void FirstUpgradeTakesAboutTenTaps()
        {
            int taps = 0;
            while (!eco.CanBuy(0)) { eco.AddEnergy(eco.ClickValue); taps++; }
            Assert.That(taps, Is.InRange(8, 12));
            Assert.IsTrue(eco.Buy(0));
            Assert.AreEqual(2, eco.ClickValue);
        }

        [Test]
        public void CostFollowsCeilFormula()
        {
            var u = cfg.upgrades[1];
            state.SetLevel(u.id, 3);
            double expected = System.Math.Ceiling(u.baseCost * System.Math.Pow(u.costGrowth, 3));
            Assert.AreEqual(expected, eco.Cost(1));
        }

        [Test]
        public void LockedUpgradeCannotBeBoughtEvenWithMoney()
        {
            state.energy = 1e9;
            int idx = cfg.UpgradeIndex("rede");
            Assert.IsFalse(eco.IsUnlocked(idx));
            Assert.IsFalse(eco.Buy(idx));
        }

        [Test]
        public void ProducedCountsSpentEnergy()
        {
            eco.AddEnergy(100);
            eco.Buy(0);
            Assert.AreEqual(100, state.campaignProduced);
            Assert.Less(state.energy, 100);
        }

        [Test]
        public void EventsStartOnlyAfterPassiveUpgrade()
        {
            ev.Tick(500);
            Assert.IsFalse(state.eventsEnabled);
            state.SetLevel("turbina", 1);
            ev.Tick(0.1f);
            Assert.IsTrue(state.eventsEnabled);
            Assert.That(state.eventTimer, Is.InRange(cfg.eventIntervalMin * 0.8f, cfg.eventIntervalMax * 1.15f));
        }

        [Test]
        public void OnlyOnePendingEventAtATime()
        {
            state.SetLevel("turbina", 1);
            ev.Tick(0.1f);
            ev.Tick(state.eventTimer + 0.1f);
            Assert.IsTrue(state.HasPendingEvent);
            string first = state.pendingEventId;
            ev.Tick(cfg.eventIntervalMax); // não sorteia outro enquanto há pendente
            Assert.AreEqual(first, state.pendingEventId);
        }

        [Test]
        public void TurbineFailureAppliesTemporaryReductionAndRecovers()
        {
            state.SetLevel("turbina", 5);
            double before = eco.PassivePerSecond;
            ev.StartEvent(cfg.FindEvent("falha_turbina"));
            Assert.IsTrue(ev.Choose(1)); // consertar com a equipe
            Assert.Less(eco.PassivePerSecond, before);
            Assert.Greater(eco.PassivePerSecond, 0);
            ev.Tick(121);
            Assert.AreEqual(before, eco.PassivePerSecond, 1e-6);
        }

        [Test]
        public void PaidChoiceRequiresEnergyAndRaisesReputation()
        {
            ev.StartEvent(cfg.FindEvent("ato_ambiental"));
            Assert.IsFalse(ev.Choose(0));
            state.energy = 1e6;
            float rep = state.reputation;
            Assert.IsTrue(ev.Choose(0));
            Assert.Greater(state.reputation, rep);
            Assert.IsFalse(state.HasPendingEvent);
        }

        [Test]
        public void TemporaryReputationPenaltyExpires()
        {
            ev.StartEvent(cfg.FindEvent("ato_ambiental"));
            Assert.IsTrue(ev.Choose(1));
            Assert.AreEqual(38, ev.EffectiveReputation, 0.01f);
            ev.Tick(181);
            Assert.AreEqual(50, ev.EffectiveReputation, 0.01f);
        }

        [Test]
        public void PressRequiresGoodReputation()
        {
            var press = cfg.FindEvent("imprensa");
            Assert.AreEqual(0, ev.WeightFor(press, 50));
            Assert.Greater(ev.WeightFor(press, 80), 0);
        }

        [Test]
        public void IgnoredEventResolvesWithDefault()
        {
            ev.StartEvent(cfg.FindEvent("falha_turbina"));
            ev.Tick(0.01f); // eventos ainda desabilitados: não expira
            state.eventsEnabled = true;
            ev.Tick(cfg.pendingEventExpire + 1);
            Assert.IsFalse(state.HasPendingEvent);
            Assert.AreEqual(1, state.effects.Count);
        }

        [Test]
        public void RebirthFormulaAndReset()
        {
            eco.AddEnergy(24999);
            Assert.IsFalse(rb.CanRebirth);
            eco.AddEnergy(1);
            Assert.AreEqual(1, rb.PendingNucleos);
            Assert.AreEqual(100000, rb.NextNucleoAt);
            state.SetLevel("painel", 5);
            state.reputation = 80;
            state.effects.Add(new ActiveEffect { kind = EffectKind.PassiveMultiplier, value = 2, remaining = 10 });
            Assert.AreEqual(1, rb.Perform());
            Assert.AreEqual(1, state.nucleos);
            Assert.AreEqual(0, state.energy);
            Assert.AreEqual(0, state.campaignProduced);
            Assert.AreEqual(0, eco.Level(0));
            Assert.AreEqual(cfg.startReputation, state.reputation);
            Assert.AreEqual(0, state.effects.Count);
            Assert.AreEqual(2, state.generation);
            Assert.AreEqual(25000, state.totalProducedAllTime);
        }

        [Test]
        public void NucleosScaleWithSqrt()
        {
            Assert.AreEqual(2, rb.NucleosFor(100000));
            Assert.AreEqual(1, rb.NucleosFor(99999));
            Assert.AreEqual(3, rb.NucleosFor(225000));
        }

        [Test]
        public void EachPermanentUpgradeCostsOneNucleoAndHelps()
        {
            foreach (var p in cfg.permanentUpgrades) Assert.AreEqual(1, rb.PermanentCost(p));

            state.nucleos = 1;
            var click = cfg.permanentUpgrades.Find(p => p.kind == PermanentKind.ClickBonus);
            Assert.IsTrue(rb.BuyPermanent(click));
            Assert.AreEqual(2, eco.ClickValue);

            state.nucleos = 1;
            double cost = eco.Cost(0);
            var disc = cfg.permanentUpgrades.Find(p => p.kind == PermanentKind.EarlyDiscount);
            Assert.IsTrue(rb.BuyPermanent(disc));
            Assert.Less(eco.Cost(0), cost);

            state.nucleos = 1;
            state.SetLevel("turbina", 2);
            double passive = eco.PassivePerSecond;
            var pas = cfg.permanentUpgrades.Find(p => p.kind == PermanentKind.PassiveBonus);
            Assert.IsTrue(rb.BuyPermanent(pas));
            Assert.Greater(eco.PassivePerSecond, passive);
        }

        [Test]
        public void SaveRoundTripKeepsProgressAndEffects()
        {
            const string file = "nucleovivo_test_save.json";
            state.energy = 1234.5;
            state.campaignProduced = 5000;
            state.SetLevel("turbina", 3);
            state.pendingEventId = "audiencia";
            state.effects.Add(new ActiveEffect { kind = EffectKind.ClickMultiplier, value = 2, remaining = 33, duration = 60, label = "x" });
            state.nucleos = 4;
            SaveSystem.Save(state, file);
            var loaded = SaveSystem.Load(cfg, file);
            SaveSystem.Delete(file);
            Assert.AreEqual(1234.5, loaded.energy, 1e-6);
            Assert.AreEqual(3, loaded.GetLevel("turbina"));
            Assert.AreEqual("audiencia", loaded.pendingEventId);
            Assert.AreEqual(1, loaded.effects.Count);
            Assert.AreEqual(33, loaded.effects[0].remaining, 1e-4);
            Assert.AreEqual(4, loaded.nucleos);
        }

        [Test]
        public void NumberFormatIsPortuguese()
        {
            Assert.AreEqual("950", NumberFormat.Short(950));
            Assert.AreEqual("1,2 mil", NumberFormat.Short(1234));
            Assert.AreEqual("3,5 mi", NumberFormat.Short(3_500_000));
            Assert.AreEqual("125 mil", NumberFormat.Short(125_000));
            Assert.AreEqual("1 mi", NumberFormat.Short(999_990));
            Assert.AreEqual("25.000", NumberFormat.Full(25000));
            Assert.AreEqual("1 min 30 s", NumberFormat.Duration(90));
        }

        [Test]
        public void SimulatedFirstRebirthTakesTenToFifteenMinutesAtTwoTapsPerSecond()
        {
            float t = SimulateToRebirth(2f);
            Assert.That(t / 60f, Is.InRange(9f, 15f), "Minutos até o primeiro Renascimento: " + (t / 60f));
        }

        [Test]
        public void SecondCampaignIsFasterWithAnyPermanentUpgrade()
        {
            float first = SimulateToRebirth(2f);
            foreach (var p in cfg.permanentUpgrades)
            {
                state = new GameState { reputation = 50 };
                state.SetPermanent(p.id, 1);
                float second = SimulateToRebirth(2f, reset: false);
                Assert.Less(second, first * 0.95f, p.displayName);
            }
        }

        /// <summary>Jogador simples: toca N vezes por segundo e compra a melhoria com melhor retorno.</summary>
        float SimulateToRebirth(float tapsPerSecond, bool reset = true)
        {
            if (reset) state = new GameState { reputation = 50 };
            float t = 0, dt = 0.25f;
            while (!rb.CanRebirth && t < 3600)
            {
                eco.AddEnergy((eco.ClickValue * tapsPerSecond + eco.PassivePerSecond) * dt);
                t += dt;
                while (true)
                {
                    int best = -1; double bestPayback = double.MaxValue; double minPayback = double.MaxValue;
                    for (int i = 0; i < cfg.upgrades.Count; i++)
                    {
                        if (!eco.IsUnlocked(i)) continue;
                        double gain = eco.ClickGainPreview(i) * tapsPerSecond + eco.PassiveGainPreview(i);
                        double pb = eco.Cost(i) / gain;
                        minPayback = System.Math.Min(minPayback, pb);
                        if (eco.CanBuy(i) && pb < bestPayback) { bestPayback = pb; best = i; }
                    }
                    if (best < 0 || bestPayback > minPayback * 1.6) break;
                    eco.Buy(best);
                }
            }
            return t;
        }
    }
}
