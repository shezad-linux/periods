using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lumenfall.Core;
using Lumenfall.Data;
using UnityEngine;

namespace Lumenfall.Services
{
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int saveVersion;
        public string checksum = string.Empty;
        public string payloadJson = string.Empty;
    }

    public sealed class SaveService : ServiceBehaviour
    {
        protected override Type ServiceType => typeof(SaveService);

        public event Action<int> SlotSaved;

        public string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, "saves");

        public void EnsureSaveDirectory()
        {
            Directory.CreateDirectory(SaveDirectoryPath);
        }

        public bool HasSlotData(int slotIndex)
        {
            return File.Exists(GetPrimarySlotPath(slotIndex));
        }

        public SaveGameData CreateNewGame(int slotIndex)
        {
            SaveGameData data = SaveGameData.CreateDefault(slotIndex);
            SaveSlot(slotIndex, data);
            return data;
        }

        public SaveGameData LoadSlot(int slotIndex)
        {
            EnsureSaveDirectory();

            if (TryReadSlot(GetPrimarySlotPath(slotIndex), out SaveGameData primaryData))
            {
                return primaryData;
            }

            if (TryReadSlot(GetBackupSlotPath(slotIndex), out SaveGameData backupData))
            {
                return backupData;
            }

            return SaveGameData.CreateDefault(slotIndex);
        }

        public bool SaveSlot(int slotIndex, SaveGameData data)
        {
            if (data == null)
            {
                return false;
            }

            EnsureSaveDirectory();

            string primaryPath = GetPrimarySlotPath(slotIndex);
            string backupPath = GetBackupSlotPath(slotIndex);
            string tempPath = GetTempSlotPath(slotIndex);
            string payload = JsonUtility.ToJson(data, true);
            SaveEnvelope envelope = new()
            {
                saveVersion = data.saveVersion,
                checksum = ComputeChecksum(payload),
                payloadJson = payload
            };

            string json = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (!TryReadSlot(tempPath, out _))
            {
                return false;
            }

            if (File.Exists(primaryPath))
            {
                File.Copy(primaryPath, backupPath, true);
            }

            File.Copy(tempPath, primaryPath, true);
            File.Delete(tempPath);
            SlotSaved?.Invoke(slotIndex);
            return true;
        }

        public string GetPrimarySlotPath(int slotIndex)
        {
            return Path.Combine(SaveDirectoryPath, $"slot_{slotIndex}.json");
        }

        public string GetBackupSlotPath(int slotIndex)
        {
            return Path.Combine(SaveDirectoryPath, $"slot_{slotIndex}.bak.json");
        }

        public string GetTempSlotPath(int slotIndex)
        {
            return Path.Combine(SaveDirectoryPath, $"slot_{slotIndex}.tmp.json");
        }

        private bool TryReadSlot(string path, out SaveGameData data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(json);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.payloadJson))
                {
                    return false;
                }

                if (!string.Equals(envelope.checksum, ComputeChecksum(envelope.payloadJson), StringComparison.Ordinal))
                {
                    Debug.LogWarning($"Save checksum mismatch for '{path}'.");
                    return false;
                }

                data = JsonUtility.FromJson<SaveGameData>(envelope.payloadJson);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read save file '{path}': {exception.Message}");
                return false;
            }
        }

        private static string ComputeChecksum(string payload)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            StringBuilder builder = new(hash.Length * 2);
            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
