using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Models
{
    [Table("SetupShortcut")]
    public class ShortcutModel : BindableBase
    {
        [Key]
        private int _id;
        private string _shortcutIconFile = string.Empty;
        private string _shortcutProcFile = string.Empty;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }
        public string ShortcutIconFile
        {
            get { return _shortcutIconFile; }
            set { SetProperty(ref _shortcutIconFile, value); }
        }
        public string ShortcutProcFile
        {
            get { return _shortcutProcFile; }
            set { SetProperty(ref _shortcutProcFile, value); }
        }
    }
}
