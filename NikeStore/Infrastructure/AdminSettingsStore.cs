using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Web.Hosting;
using Newtonsoft.Json;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Infrastructure
{
    public sealed class AdminSettingsStore
    {
        private static readonly object Gate = new object();
        private readonly string path;

        public AdminSettingsStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Thiếu đường dẫn cấu hình.", "path");
            this.path = path;
        }

        public static AdminSettingsStore Current()
        {
            return new AdminSettingsStore(HostingEnvironment.MapPath("~/App_Data/AdminSettings.json"));
        }

        public AdminSettings Load()
        {
            lock (Gate)
            {
                if (!File.Exists(path)) return new AdminSettings();
                try
                {
                    var model = JsonConvert.DeserializeObject<AdminSettings>(File.ReadAllText(path, Encoding.UTF8));
                    var errors = new List<ValidationResult>();
                    if (model != null && Validator.TryValidateObject(model, new ValidationContext(model), errors, true))
                        return model;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (JsonException) { }
                return new AdminSettings();
            }
        }

        public void Save(AdminSettings model)
        {
            if (model == null) throw new ArgumentNullException("model");
            model.DisplayName = (model.DisplayName ?? "").Trim();
            Validator.ValidateObject(model, new ValidationContext(model), true);
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temporary, JsonConvert.SerializeObject(model, Formatting.Indented), new UTF8Encoding(false));
                    if (File.Exists(path)) File.Replace(temporary, path, null);
                    else File.Move(temporary, path);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
        }
    }
}
