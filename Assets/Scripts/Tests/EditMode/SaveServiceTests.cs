using System.IO;
using Lumenfall.Data;
using Lumenfall.Services;
using NUnit.Framework;
using UnityEngine;

namespace Lumenfall.Tests.EditMode
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void SaveService_RoundTripsAndFallsBackToBackup()
        {
            GameObject root = new("SaveServiceTest");
            SaveService saveService = root.AddComponent<SaveService>();

            SaveGameData saveData = SaveGameData.CreateDefault(1);
            saveData.UnlockAbility(AbilityType.DashCore);
            Assert.That(saveService.SaveSlot(1, saveData), Is.True);

            string primaryPath = saveService.GetPrimarySlotPath(1);
            string backupPath = saveService.GetBackupSlotPath(1);

            SaveGameData updated = SaveGameData.CreateDefault(1);
            updated.UnlockAbility(AbilityType.PhaseShift);
            Assert.That(saveService.SaveSlot(1, updated), Is.True);
            Assert.That(File.Exists(backupPath), Is.True);

            File.WriteAllText(primaryPath, "corrupted");
            SaveGameData loaded = saveService.LoadSlot(1);

            Assert.That(loaded.HasAbility(AbilityType.DashCore), Is.True);

            Object.DestroyImmediate(root);
        }
    }
}
