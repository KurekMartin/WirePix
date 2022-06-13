using MediaDevices;
using PhotoApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoApp
{

    public class DeviceFileInfo : BaseObserveObject
    {
        private struct BaseFileInfo
        {
            public BaseFileInfo(string PersistentUniqueId, string fullPath)
            {
                this.PersistentUniqueId = PersistentUniqueId;
                this.FullPath = fullPath;
            }
            public string PersistentUniqueId;
            public string FullPath;
        }

        private List<BaseFileInfo> _allFilesInfo = new List<BaseFileInfo>();
        private List<string> _allFileTypes = new List<string>();
        private IEnumerable<string> _images;
        private IEnumerable<string> _videos;
        private IEnumerable<string> _others;

        public DeviceFileInfo(IEnumerable<MediaFileInfo> mediaFiles = null)
        {
            if (mediaFiles != null)
            {
                _allFilesInfo = mediaFiles.Select(f => new BaseFileInfo(f.PersistentUniqueId, f.FullName)).ToList();
            }
        }

        public int AllFilesCount
        {
            get
            {
                return _allFilesInfo.Count();
            }
        }

        public List<string> AllFileTypes
        {
            get
            {
                if (_allFileTypes.Count() == 0 && AllFilesCount > 0)
                {
                    _allFileTypes = _allFilesInfo.Select(f => Path.GetExtension(f.FullPath).ToUpper()).Distinct().ToList();
                }
                return _allFileTypes;
            }
        }

        public IEnumerable<string> Images
        {
            get
            {
                if (_images == null)
                {
                    _images = AllFileTypes.Where(f => FileType.IsImage(f));
                }
                return _images;
            }
        }
        public IEnumerable<string> Videos
        {
            get
            {
                if (_videos == null)
                {
                    _videos = AllFileTypes.Where(f => FileType.IsVideo(f));
                }
                return _videos;
            }
        }
        public IEnumerable<string> Others
        {
            get
            {
                if (_others == null)
                {
                    _others = AllFileTypes.Except(_images.Concat(_videos));
                }
                return _others;
            }
        }
    }
}
