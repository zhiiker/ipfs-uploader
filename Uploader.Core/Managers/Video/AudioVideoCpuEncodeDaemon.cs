using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class AudioVideoCpuEncodeDaemon : BaseDaemon
    {
        public static AudioVideoCpuEncodeDaemon Instance { get; private set; }

        static AudioVideoCpuEncodeDaemon()
        {
            Instance = new AudioVideoCpuEncodeDaemon();
            Instance.Start(VideoSettings.Instance.NbAudioVideoCpuEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if (!fileItem.AudioVideoCpuEncodeProcess.CanProcess())
            {
                string message = "FileName " + Path.GetFileName(fileItem.OutputFilePath) + " car le client est déconnecté";
                LogManager.AddEncodingMessage(LogLevel.Warning, message, "Annulation");
                fileItem.AudioVideoCpuEncodeProcess.CancelCascade("Le client est déconnecté.");
                return;
            }

            if (EncodeManager.AudioVideoCpuEncoding(fileItem))
            {
                // rechercher le 480p pour le sprite
                if(fileItem.VideoSize == VideoSize.F480p && fileItem.FileContainer.SpriteVideoFileItem != null)
                {
                    fileItem.FileContainer.SpriteVideoFileItem.SetSourceFilePath(fileItem.OutputFilePath);
                    SpriteDaemon.Instance.Queue(fileItem.FileContainer.SpriteVideoFileItem, "Waiting sprite creation...");
                }

                IpfsDaemon.Instance.Queue(fileItem);
            }
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddEncodingMessage(LogLevel.Critical, ex.ToString(), "Exception non gérée");                        
            fileItem.AudioVideoCpuEncodeProcess.SetErrorMessage("Exception non gérée");
        }

        public void Queue(FileItem fileItem, string messageIpfs)
        {
            base.Queue(fileItem, fileItem.AudioVideoCpuEncodeProcess);

            fileItem.IpfsProcess.SetProgress(messageIpfs, true);
        }
    }
}