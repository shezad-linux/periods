using Lumenfall.Data;
using NUnit.Framework;

namespace Lumenfall.Tests.EditMode
{
    public sealed class GateRequirementTests
    {
        [Test]
        public void GateRequirement_ReturnsTrue_WhenAllAbilitiesAndFlagsExist()
        {
            SaveGameData saveData = SaveGameData.CreateDefault(0);
            saveData.UnlockAbility(AbilityType.CrystalGrapple);
            saveData.MarkBossDefeated("boss_merchant");
            saveData.AddWorldFlag("archives_open");

            GateRequirement requirement = new();
            requirement.requiredAbilities.Add(AbilityType.CrystalGrapple);
            requirement.requiredBossIds.Add("boss_merchant");
            requirement.requiredFlags.Add("archives_open");

            Assert.That(requirement.IsSatisfied(saveData), Is.True);
        }
    }
}
