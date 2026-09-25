using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;

namespace WhatsAppClone.Services
{
    public enum LoginResult { Success, InvalidCredentials, MissingFile, UnreadableFile }
    public enum RegistrationResult { Success, DuplicateAccount, UnreadableFile }
    public enum EmailLookupResult { Found, NotFound, MissingFile, UnreadableFile }

    // Account data lives in the app's private local folder, never in the installation folder.
    public static class CredentialStore
    {
        private const string FileName = "users.json";
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        public static async Task<LoginResult> LoginAsync(string identifier, string password)
        {
            try
            {
                JsonArray users = await ReadUsersAsync(false);
                foreach (IJsonValue value in users)
                {
                    JsonObject user = value.GetObject();
                    bool matches = string.Equals(user.GetNamedString("email"), identifier, StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(user.GetNamedString("phone"), identifier, StringComparison.Ordinal);
                    if (matches && string.Equals(user.GetNamedString("passwordHash"),
                            HashPassword(user.GetNamedString("salt"), password), StringComparison.Ordinal))
                    {
                        return LoginResult.Success;
                    }
                }
                return LoginResult.InvalidCredentials;
            }
            catch (FileNotFoundException) { return LoginResult.MissingFile; }
            catch (Exception) { return LoginResult.UnreadableFile; }
        }

        public static async Task<RegistrationResult> RegisterAsync(string name, string phone, string email, string password)
        {
            await Gate.WaitAsync();
            try
            {
                JsonArray users = await ReadUsersAsync(true);
                bool exists = users.Any(value =>
                    string.Equals(value.GetObject().GetNamedString("email"), email, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value.GetObject().GetNamedString("phone"), phone, StringComparison.Ordinal));
                if (exists) return RegistrationResult.DuplicateAccount;

                string salt = CryptographicBuffer.EncodeToHexString(CryptographicBuffer.GenerateRandom(16));
                var account = new JsonObject
                {
                    ["name"] = JsonValue.CreateStringValue(name),
                    ["phone"] = JsonValue.CreateStringValue(phone),
                    ["email"] = JsonValue.CreateStringValue(email),
                    ["salt"] = JsonValue.CreateStringValue(salt),
                    ["passwordHash"] = JsonValue.CreateStringValue(HashPassword(salt, password))
                };
                users.Add(account);
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    FileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, users.Stringify());
                return RegistrationResult.Success;
            }
            catch (Exception) { return RegistrationResult.UnreadableFile; }
            finally { Gate.Release(); }
        }

        public static async Task<EmailLookupResult> FindEmailAsync(string email)
        {
            try
            {
                JsonArray users = await ReadUsersAsync(false);
                return users.Any(value => string.Equals(value.GetObject().GetNamedString("email"),
                    email, StringComparison.OrdinalIgnoreCase)) ? EmailLookupResult.Found : EmailLookupResult.NotFound;
            }
            catch (FileNotFoundException) { return EmailLookupResult.MissingFile; }
            catch (Exception) { return EmailLookupResult.UnreadableFile; }
        }

        private static async Task<JsonArray> ReadUsersAsync(bool allowMissing)
        {
            IStorageItem item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(FileName);
            if (item == null)
            {
                if (allowMissing) return new JsonArray();
                throw new FileNotFoundException("No local accounts file exists yet.");
            }
            if (!(item is StorageFile file)) throw new InvalidDataException("Account data is not a file.");

            var parsed = JsonValue.Parse(await FileIO.ReadTextAsync(file));
            if (parsed.ValueType != JsonValueType.Array) throw new InvalidDataException("Account data has the wrong format.");
            JsonArray users = parsed.GetArray();
            foreach (IJsonValue value in users)
            {
                JsonObject user = value.GetObject();
                user.GetNamedString("name");
                user.GetNamedString("phone");
                user.GetNamedString("email");
                user.GetNamedString("salt");
                user.GetNamedString("passwordHash");
            }
            return users;
        }

        private static string HashPassword(string salt, string password)
        {
            var provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            var bytes = CryptographicBuffer.ConvertStringToBinary(salt + ":" + password, BinaryStringEncoding.Utf8);
            return CryptographicBuffer.EncodeToHexString(provider.HashData(bytes));
        }
    }
}
