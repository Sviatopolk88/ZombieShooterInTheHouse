using System.Collections.Generic;
using System.Reflection;
using Modules.HealthSystem;
using NUnit.Framework;
using UnityEngine;

namespace Project.HealthSystem.EditModeTests
{
    public sealed class HealthTests
    {
        private readonly List<GameObject> spawnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                Object.DestroyImmediate(spawnedObjects[i]);
            }

            spawnedObjects.Clear();
        }

        [Test]
        public void InitialState_IsAliveAndFullHealth()
        {
            Health health = CreateHealth();

            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.CanTakeDamage, Is.True);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            Health health = CreateHealth();

            bool result = health.TakeDamage(new DamageContext(25));

            Assert.That(result, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(75));
            Assert.That(health.IsAlive, Is.True);
        }

        [Test]
        public void TakeDamage_IntOverload_DelegatesToContextVersion()
        {
            Health health = CreateHealth();

            health.TakeDamage(10);

            Assert.That(health.CurrentHealth, Is.EqualTo(90));
            Assert.That(health.LastDamageContext.Amount, Is.EqualTo(10));
            Assert.That(health.LastDamageContext.DamageType, Is.EqualTo(DamageType.Default));
        }

        [Test]
        public void TakeDamage_NonPositiveAmount_IsIgnored()
        {
            Health health = CreateHealth();

            bool negativeResult = health.TakeDamage(new DamageContext(-10));
            bool zeroResult = health.TakeDamage(new DamageContext(0));

            Assert.That(negativeResult, Is.False);
            Assert.That(zeroResult, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Heal_RestoresHealthButNotAboveMax()
        {
            Health health = CreateHealth();
            health.TakeDamage(30);

            health.Heal(20);
            Assert.That(health.CurrentHealth, Is.EqualTo(90));

            health.Heal(50);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Heal_NonPositiveAmount_IsIgnored()
        {
            Health health = CreateHealth();
            health.TakeDamage(20);

            health.Heal(-10);
            health.Heal(0);

            Assert.That(health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void LethalDamage_KillsObject_AndOnDeathFiresOnce()
        {
            Health health = CreateHealth();
            int deathCalls = 0;
            health.OnDeath += () => deathCalls++;

            bool result = health.TakeDamage(new DamageContext(100));
            health.TakeDamage(10);
            health.Kill();

            Assert.That(result, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(deathCalls, Is.EqualTo(1));
        }

        [Test]
        public void Heal_AfterDeath_IsIgnored()
        {
            Health health = CreateHealth();
            health.Kill();

            health.Heal(50);

            Assert.That(health.CurrentHealth, Is.EqualTo(0));
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void Kill_FiresOnlyHealthChangedAndDeath()
        {
            Health health = CreateHealth();
            int damageAppliedCalls = 0;
            int damagedCalls = 0;
            int healthChangedCalls = 0;
            int deathCalls = 0;

            health.OnDamageApplied += (_, _) => damageAppliedCalls++;
            health.OnDamaged += _ => damagedCalls++;
            health.OnHealthChanged += (_, _) => healthChangedCalls++;
            health.OnDeath += () => deathCalls++;

            health.Kill();

            Assert.That(damageAppliedCalls, Is.EqualTo(0));
            Assert.That(damagedCalls, Is.EqualTo(0));
            Assert.That(healthChangedCalls, Is.EqualTo(1));
            Assert.That(deathCalls, Is.EqualTo(1));
        }

        [Test]
        public void ResetHealth_RestoresHealth_Revives_AndClearsLastDamageContext()
        {
            Health health = CreateHealth();
            GameObject source = new("Source");
            spawnedObjects.Add(source);

            health.TakeDamage(new DamageContext(20, DamageType.Fire, source, true, HitZone.Head));
            health.Kill();

            health.ResetHealth();

            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.LastDamageContext.Amount, Is.EqualTo(0));
            Assert.That(health.LastDamageContext.Source, Is.Null);
        }

        [Test]
        public void SetMaxHealth_ClampsCurrentHealthToRange()
        {
            Health health = CreateHealth();
            health.TakeDamage(20);

            health.SetMaxHealth(50, fillHealth: false);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
            Assert.That(health.MaxHealth, Is.EqualTo(50));

            health.SetMaxHealth(120, fillHealth: true);
            Assert.That(health.CurrentHealth, Is.EqualTo(120));
            Assert.That(health.MaxHealth, Is.EqualTo(120));
        }

        [Test]
        public void Invulnerable_BlocksDamageAndCanTakeDamage()
        {
            Health health = CreateHealth();
            SetPrivateField(health, "invulnerable", true);

            bool canApplyDamage = health.CanApplyDamage(new DamageContext(10));
            bool result = health.TakeDamage(new DamageContext(10));

            Assert.That(health.CanTakeDamage, Is.False);
            Assert.That(canApplyDamage, Is.False);
            Assert.That(result, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void CanApplyDamage_ReturnsFalseForInvalidContext()
        {
            Health health = CreateHealth();

            Assert.That(health.CanApplyDamage(new DamageContext(10)), Is.True);
            Assert.That(health.CanApplyDamage(new DamageContext(0)), Is.False);
            Assert.That(health.CanApplyDamage(new DamageContext(-5)), Is.False);

            health.Kill();

            Assert.That(health.CanApplyDamage(new DamageContext(10)), Is.False);
        }

        [Test]
        public void TakeDamage_EventOrder_IsDeterministic()
        {
            Health health = CreateHealth();
            List<string> events = new();

            health.OnDamageApplied += (_, _) => events.Add("OnDamageApplied");
            health.OnDamaged += _ => events.Add("OnDamaged");
            health.OnHealthChanged += (_, _) => events.Add("OnHealthChanged");
            health.OnDeath += () => events.Add("OnDeath");

            health.TakeDamage(new DamageContext(100));

            CollectionAssert.AreEqual(
                new[] { "OnDamageApplied", "OnDamaged", "OnHealthChanged", "OnDeath" },
                events);
        }

        [Test]
        public void Heal_EventOrder_IsDeterministic()
        {
            Health health = CreateHealth();
            List<string> events = new();
            health.TakeDamage(20);

            health.OnHealed += _ => events.Add("OnHealed");
            health.OnHealthChanged += (_, _) => events.Add("OnHealthChanged");

            health.Heal(10);

            CollectionAssert.AreEqual(
                new[] { "OnHealed", "OnHealthChanged" },
                events);
        }

        private Health CreateHealth(int maxHealth = 100)
        {
            GameObject gameObject = new("Health_Test_Object");
            spawnedObjects.Add(gameObject);

            Health health = gameObject.AddComponent<Health>();
            health.SetMaxHealth(maxHealth);
            health.ResetHealth();
            return health;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }
    }
}
