using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace WPFColorPicker
{
    class ColorPickerViewModel : ViewModelBase
    {
        string _dirFullPath;
        string _fileFullPath;

        public ColorPickerViewModel()
        {
            Init();


            GetColors();

            IsAddPanelVsb = false;
            IsEditPanelVsb = false;
        }

        private void Init()
        {
            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = "WPFColorPicker";

            _dirFullPath = Path.Combine(rootPath, dir);

            if (!Directory.Exists(_dirFullPath))
            {
                Directory.CreateDirectory(_dirFullPath);
            }

            string filename = "ColorList.json";

            _fileFullPath = Path.Combine(_dirFullPath, filename);

            if (!File.Exists(_fileFullPath))
            {
                File.Create(_fileFullPath).Close();

                var colors = GetDefaultColors();

                MyColors = new ObservableCollection<WPFColor>();
                foreach (KeyValuePair<string, string> kvp in colors)
                {
                    MyColors.Add(new WPFColor
                    {
                        Name = kvp.Key,
                        Hex = kvp.Value
                    });
                }

                string updatedJson = JsonConvert.SerializeObject(MyColors, Formatting.Indented);

                // 寫入檔案
                File.WriteAllText(_fileFullPath, updatedJson);
            }
        }

        private Dictionary<string, string> GetDefaultColors()
        {
            return new Dictionary<string, string>
            {
                {"white-1", "white" },
                {"white-2", "#e6e6f0" },

                {"gray-1", "#64646E" },
                {"gray-2", "#464650" },
                {"gray-3", "#32323C" },
                {"gray-4", "#282832" },

                {"dark-1", "#001111" },
                {"dark-2", "Black" },
                {"dark-3", "#0f0f19" },
                {"dark-4", "#1E1E28" },

                {"blue-1", "#88DDEE" },
                {"blue-2", "#2277BB" },

                {"yellow-1", "#FFDD00" },

                {"green-1", "#80CE2E" },
                {"green-2", "#6ab82d" },
                {"green-3", "#326400" },

                {"red-1", "#FFCC2F2F" },
            };
        }

        private void GetColors()
        {
            string jsonData = File.ReadAllText(_fileFullPath);

            List<WPFColor> colors = JsonConvert.DeserializeObject<List<WPFColor>>(jsonData) ?? new List<WPFColor>();

            MyColors = new ObservableCollection<WPFColor>(colors);
        }

        private bool _isAddPanelVsb;
        public bool IsAddPanelVsb
        {
            get => _isAddPanelVsb;
            set
            {
                Set(ref _isAddPanelVsb, value);
            }
        }


        private bool _isEditPanelVsb;
        public bool IsEditPanelVsb
        {
            get => _isEditPanelVsb;
            set
            {
                Set(ref _isEditPanelVsb, value);
            }
        }


        private string _addName;
        public string AddName
        {
            get => _addName;
            set => Set(ref _addName, value);
        }

        private string _addHex;
        public string AddHex
        {
            get => _addHex;
            set => Set(ref _addHex, value);
        }

        private ObservableCollection<WPFColor> _myColors;
        public ObservableCollection<WPFColor> MyColors
        {
            get => _myColors;
            set
            {
                Set(ref _myColors, value);
            }
        }

        WPFColor ColorSelected;

        public RelayCommand ColorSelected_Click => new RelayCommand((color) =>
        {
            ColorSelected = (WPFColor)color;
        });

        public RelayCommand ColorInsert_Click => new RelayCommand((color) =>
        {
            var model = color as WPFColor;
            InsertColorCode(model.Hex);
        });

        public RelayCommand AddConfirm_Click => new RelayCommand((x) =>
        {
            if (!(string.IsNullOrEmpty(AddName) || string.IsNullOrEmpty(AddHex)))
            {
                var hex = $"#{AddHex}";
                AddNewItemToDb(AddName, hex);

                MyColors.Add(new WPFColor
                {
                    Name = AddName,
                    Hex = hex
                });
                IsAddPanelVsb = false;
            }
            else
            {
                MessageBox.Show("Name or Hex fields can not empty");
            }
        });

        public RelayCommand AddCancel_Click => new RelayCommand((x) =>
        {
            IsAddPanelVsb = false;
        });

        public RelayCommand EditConfirm_Click => new RelayCommand((x) =>
        {
            if (!(string.IsNullOrEmpty(AddName) || string.IsNullOrEmpty(AddHex)))
            {
                var hex = $"#{AddHex}";

                var index = MyColors.IndexOf(ColorSelected);

                UpdateItemToDb(index, AddName, hex);

                MyColors[index] = new WPFColor
                {
                    Name = AddName,
                    Hex = hex
                };

                IsEditPanelVsb = false;
            }
            else
            {
                MessageBox.Show("Name or Hex fields can not empty");
            }
        });

        public RelayCommand EditCancel_Click => new RelayCommand((x) =>
        {
            AddName = "";
            AddHex = "";

            IsEditPanelVsb = false;
        });


        public RelayCommand AddColor_Click => new RelayCommand((x) =>
        {
            IsAddPanelVsb = true;
        });

        public RelayCommand EditColor_Click => new RelayCommand((x) =>
        {
            if (ColorSelected != null)
            {
                AddName = ColorSelected.Name;
                AddHex = ColorSelected.Hex.Replace("#", "");

                IsEditPanelVsb = true;
            }
        });

        public RelayCommand DeleteColor_Click => new RelayCommand((x) =>
        {
            if (ColorSelected != null)
            {
                var result = MessageBox.Show("Delete Color?", "MessageBox", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var index = MyColors.IndexOf(ColorSelected);
                    RemoveItemToDb(index);

                    MyColors.RemoveAt(index);
                    ColorSelected = null;
                }
            }
        });

        private void InsertColorCode(string colorCode)
        {
            IVsTextManager textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            textManager.GetActiveView(1, null, out IVsTextView vsTextView);

            // 轉換 IVsTextView 為 IWpfTextView
            IWpfTextView wpfTextView = GetWpfTextView(vsTextView);
            if (wpfTextView == null)
            {
                return;
            }

            // 使用 IWpfTextView 來獲取光標位置
            SnapshotPoint caretPosition = wpfTextView.Caret.Position.BufferPosition;

            // 在光標位置插入文本
            using (var edit = wpfTextView.TextBuffer.CreateEdit())
            {
                edit.Insert(caretPosition, colorCode);
                edit.Apply();
            }
        }

        private IWpfTextView GetWpfTextView(IVsTextView vsTextView)
        {
            var userData = vsTextView as IVsUserData;
            if (userData == null)
            {
                return null;
            }

            Guid guidWpfTextViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidWpfTextViewHost, out object wpfTextViewHost);
            return (wpfTextViewHost as IWpfTextViewHost)?.TextView;
        }

        private void AddNewItemToDb(string name, string hex)
        {
            // 指定檔案路徑
            string filePath = _fileFullPath;

            // 讀取並反序列化 JSON 檔案
            string jsonData = File.ReadAllText(filePath);
            List<WPFColor> color = JsonConvert.DeserializeObject<List<WPFColor>>(jsonData) ?? new List<WPFColor>();

            // 新增一個新的對象
            color.Add(new WPFColor
            {
                Name = name,
                Hex = hex
            });

            // 將更新後的對象列表序列化回 JSON
            string updatedJson = JsonConvert.SerializeObject(color, Formatting.Indented);

            // 寫入檔案
            File.WriteAllText(filePath, updatedJson);
        }

        private void UpdateItemToDb(int index, string name, string hex)
        {
            // 指定檔案路徑
            string filePath = _fileFullPath;

            // 讀取並反序列化 JSON 檔案
            string jsonData = File.ReadAllText(filePath);
            List<WPFColor> color = JsonConvert.DeserializeObject<List<WPFColor>>(jsonData) ?? new List<WPFColor>();

            color[index].Name = name;
            color[index].Hex = hex;

            // 將更新後的對象列表序列化回 JSON
            string updatedJson = JsonConvert.SerializeObject(color, Formatting.Indented);

            // 寫入檔案
            File.WriteAllText(filePath, updatedJson);
        }


        private void RemoveItemToDb(int index)
        {
            // 指定檔案路徑
            string filePath = _fileFullPath;

            // 讀取並反序列化 JSON 檔案
            string jsonData = File.ReadAllText(filePath);
            List<WPFColor> color = JsonConvert.DeserializeObject<List<WPFColor>>(jsonData) ?? new List<WPFColor>();

            color.RemoveAt(index);

            // 將更新後的對象列表序列化回 JSON
            string updatedJson = JsonConvert.SerializeObject(color, Formatting.Indented);

            // 寫入檔案
            File.WriteAllText(filePath, updatedJson);
        }
    }
}
