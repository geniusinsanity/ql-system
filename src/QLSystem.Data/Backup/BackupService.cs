using System;
using System.IO;

namespace QLSystem.Data.Backup
{
    /// <summary>
    /// مسؤول عن إنشاء نسخ احتياطية تلقائية من قاعدة بيانات المحل (Sauvegarde Automatique)
    /// </summary>
    public class BackupService
    {
        private readonly DatabaseContext _context;

        public BackupService(DatabaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// عمل نسخة احتياطية فورية وحفظها في المجلد المحدد أو فلاش ديسك
        /// </summary>
        public string PerformBackup(string? targetDirectory = null)
        {
            try
            {
                var sourcePath = _context.DatabasePath;
                if (!File.Exists(sourcePath))
                {
                    return string.Empty;
                }

                string destinationFolder;
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    destinationFolder = Path.Combine(documents, "QLSystem_Sauvegardes");
                }
                else
                {
                    destinationFolder = targetDirectory;
                }

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var backupFileName = $"QL_Backup_{timestamp}.db";
                var destinationPath = Path.Combine(destinationFolder, backupFileName);

                File.Copy(sourcePath, destinationPath, overwrite: true);
                return destinationPath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
