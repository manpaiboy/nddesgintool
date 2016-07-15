using CCWin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PlatformTools.Utilities;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;

namespace PlatformTools
{
    public partial class MyUpdatePackageFileVerification : Skin_Color
    {
        public Panel _panel { get; set; }
        Thread clientLoad;
        Thread clientBeginLoad;
        Thread packageBeginLoad;
        string flag = "";
        private static MyUpdatePackageFileVerification _instance;
        private PlatformTools.MainForm.SetHandlerToolSet m_toolSet;
        IniFileHelper inifilehelper;
        string chooseinipath = Application.StartupPath + "\\ini\\choose.ini";
        string defaultinipath = Application.StartupPath + "\\ini\\default.ini";
        #region 更新包检查需要的全局变量
        public ArrayList packagepath = new ArrayList();
        private List<string> packagefiles = new List<string>();
        //private List<string> packagefolds = new List<string>();
        string packageselectfold = "";
        #endregion

        #region 客户端检查需要的全局变量
        string myclientselectfold = "";
        public ArrayList clientpath = new ArrayList();
        private List<string> clientfiles = new List<string>();
        #endregion

        string _choosenote = "";

        #region 可选功能检查功能键值枚举
        /// <summary>
        /// 可选功能检查功能键值
        /// </summary>
        enum CHOOSEFUNCTIONKEY {
            /// <summary>
            /// 不检查可选功能
            /// </summary>
            _NOCHECKTYPE,
            /// <summary>
            /// 检查功能对应的文件是否存在，“checkn=”配置对应的需要检查的更新包文件目录及文件名字。存在，则在结果里返回true，不存在则在结果里返回FALSE。
            /// </summary>
            _CHECKTYPE1,
            /// <summary>
            /// 检查功能对应的目录是否存在，“checkn=”配置对应的需要检查的更新包文件目录。存在，则结果里返回true，不存在则在结果里返回FALSE。
            /// </summary>
            _CHECKTYPE2,
            /// <summary>
            /// 检查存在特殊文件时给予提示，“checkn=”配置对应的需要检查的更新包目录及文件名字，存在则取note配置的文字进行提示。
            /// </summary>
            _CHECKTYPE3 
        };
        #endregion

        #region 默认功能检查功能键值枚举
        /// <summary>
        /// 默认功能检查功能键值
        /// </summary>
        enum DEFAULTFUNCTIONKEY {
            /// <summary>
            /// 不检查可选功能
            /// </summary>
            _NOCHECKTYPE,
            /// <summary>
            /// 检查功能对应的文件是否存在，“checkn=”配置对应的需要检查的更新包文件目录及文件名字。存在，则在结果里返回true，不存在则在结果里返回FALSE。
            /// </summary>
            _CHECKTYPE1,
            /// <summary>
            /// 检查功能对应的目录是否存在，“checkn=”配置对应的需要检查的更新包文件目录。存在，则结果里返回true，不存在则在结果里返回FALSE。
            /// </summary>
            _CHECKTYPE2,
            /// <summary>
            /// 检查存在特殊文件时给予提示，“checkn=”配置对应的需要检查的更新包目录及文件名字，存在则取note配置的文字进行提示。
            /// </summary>
            _CHECKTYPE3,
            /// <summary>
            /// 对更新包和客户端的文件的时间对比，“checkn=”不配置，note配置提示文字，当更新包的文件的修改时间小于对比客户端文件的修改时间时则在结果里进行提示。
            /// </summary>
            _CHECKTYPE4,
            /// <summary>
            /// version.dat的版本号检查，check1=N，N表示更新包文件名字的倒数多少个字节，读取更新包客户端目录下的version.dat里面的值与之对比。
            /// 如更新包名字为“2015.01.01__魔域__6388”，check1=4，则version号是6388，如果version.dat里的值不等于6388则根据note的内容进行提示。
            /// </summary>
            _CHECKTYPE5,
            /// <summary>
            /// 客户端更新文件打包RAR（压缩方式：标准，压缩选项：创建固实压缩文件）后的大小检查，check1=N，N表示文件大小。
            /// 如N=6，则表示如果压缩包大于6M时，则根据note里的文字进行提示。
            /// </summary>
            _CHECKTYPE6 
        };
        #endregion

        #region 窗体构造器，响应事件,控件基本响应事件
        public MyUpdatePackageFileVerification()
        {
            InitializeComponent();
        }
         public static MyUpdatePackageFileVerification InstanceObject(PlatformTools.MainForm.SetHandlerToolSet toolSet)
        {
            if (_instance == null)
                _instance = new MyUpdatePackageFileVerification(toolSet);
                return _instance;
        }
        //构造函数
         public MyUpdatePackageFileVerification(PlatformTools.MainForm.SetHandlerToolSet toolset)
        {
            this.m_toolSet = toolset;
            inifilehelper = new IniFileHelper();
            InitializeComponent();
        }

         private void MyUpdatePackageFileVerification_Load(object sender, EventArgs e)
         {
             VerifiyIniExis();
             tv_default.Nodes.Clear();
             LoadFunctionList();
             textEditorControl1.Document.HighlightingStrategy = ICSharpCode.TextEditor.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("C#");
             textEditorControl1.SetAutoScrollMargin(0, 0);
             //tv_default.ExpandAll();
             clientBeginLoad = new Thread(new ThreadStart(LoadClientFilesIfIniHasConf));
             clientBeginLoad.Start();
         }

         private void MyUpdatePackageFileVerification_FormClosing(object sender, FormClosingEventArgs e)
        {
            _instance = null;
        }
         private void MyUpdatePackageFileVerification_Move(object sender, EventArgs e)
        {
            if (_panel != null)
            {
                if (_panel.Controls.Contains(this))
                {
                    Rectangle p = this.Bounds;

                    if (flag == "btn" && (p.Right > _panel.ClientSize.Width / 2 || p.Left < (-_panel.ClientSize.Width / 2)))
                    {
                        foreach (Control item in _panel.Controls)
                        {
                            if (item.GetType() == typeof(MyUpdatePackageFileVerification))
                            {
                                _panel.Controls.Remove(item);
                                _panel.Refresh();
                                ((MyUpdatePackageFileVerification)item).Btn_Seleparete.Text = "合并";
                                Point ps = new Point((MousePosition.X - _panel.ClientSize.Width), MousePosition.Y);

                                ((MyUpdatePackageFileVerification)item).SetBounds(ps.X, ps.Y, this.Width, this.Height);
                                ((MyUpdatePackageFileVerification)item).TopLevel = true;
                            }
                        }
                        this.Move -= MyUpdatePackageFileVerification_Move;
                    }
                    if (p.Left > _panel.ClientSize.Width / 4 || p.Right < (_panel.ClientSize.Width / 2 + _panel.ClientSize.Width / 4))
                    {
                        foreach (Control item in _panel.Controls)
                        {
                            if (item.GetType() == typeof(MyUpdatePackageFileVerification))
                            {
                                _panel.Controls.Remove(item);
                                _panel.Refresh();
                                ((MyUpdatePackageFileVerification)item).Btn_Seleparete.Text = "合并";
                                Point ps = new Point((MousePosition.X - _panel.ClientSize.Width), MousePosition.Y);

                                ((MyUpdatePackageFileVerification)item).SetBounds(ps.X, ps.Y, this.Width, this.Height);
                                ((MyUpdatePackageFileVerification)item).TopLevel = true;
                            }
                        }
                        this.Move -= MyUpdatePackageFileVerification_Move;
                    }
                    flag = "";
                }
            }
        }
         private void btnLoadPackage_DragEnter(object sender, DragEventArgs e)
         {
             if (e.Data.GetDataPresent(DataFormats.FileDrop))
             {
                 e.Effect = DragDropEffects.All;
                 scbLoadPackageOk.Checked = false;
                 scbLoadPackageOk.Text = "";
                 scbLoadPackageOk.Visible = false;
                 btnLoadPackage.ForeColor = Color.Black;
             }
             else
             {
                 e.Effect = DragDropEffects.None;
             }
         }

         private void btnLoadPackage_DragDrop(object sender, DragEventArgs e)
         {
             string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
             string curfold = "";
             for (int i = 0; i < s.Length; i++)
             {
                 curfold = s[i];
             }
             if (curfold.IndexOf('\\') > 0)
             {
                 if (File.Exists(curfold))
                 {
                     // 是文件
                     btnLoadPackage.ForeColor = Color.Red;
                     labPackagePath.Text = "选择错误，这是个文件，不是更新包！";
                     labPackagePathReal.Text = "选择错误，这是个文件，不是更新包！";
                     scbLoadPackageOk.Checked = false;
                     scbLoadPackageOk.Text = "";
                     scbLoadPackageOk.Visible = false;
                 }
                 else if (Directory.Exists(curfold))
                 {
                     // 是文件夹
                     btnLoadPackage.ForeColor = Color.FromArgb(0, 51, 161, 224);
                     //getAllPackageFiles(curfold);
                     //scbLoadPackageOk.Checked = true;
                     //scbLoadPackageOk.Text = "载入成功";
                     ////Thread.Sleep(800);
                     ////scbLoadPackageOk.Text = "";
                     //scbLoadPackageOk.Visible = true;
                     labPackagePath.Text = curfold;
                     if (labPackagePath.Text.Length >= 25)
                     {
                         labPackagePathReal.Text = labPackagePath.Text.Substring(0, 25) + "...";
                     }
                     else
                     {
                         labPackagePathReal.Text = labPackagePath.Text;
                     }
                     packageselectfold = curfold;
                     packageBeginLoad = new Thread(new ThreadStart(aysnBeginShowProgress));
                     packageBeginLoad.Start();
                 }
                 else
                 {
                     // 都不是
                 }
             }
         }

         private void labPackagePathReal_MouseHover(object sender, EventArgs e)
         {
             toolStripGlobalControl.Show("更新包路径：" + packageselectfold, labPackagePathReal);
         }

         private void vsbScrollTree_Scroll(object sender, ScrollEventArgs e)
         {
             tv_default.AutoScrollOffset = new Point(e.OldValue, e.NewValue);
         }

         private void tv_default_AfterCheck(object sender, TreeViewEventArgs e)
         {
             #region 默认功能树联动选择
             try
             {
                 if (e.Node.Nodes.Count > 0)
                 {
                     bool NoFalse = true;
                     foreach (TreeNode tn in e.Node.Nodes)
                     {
                         if (tn.Checked == false)
                         {
                             NoFalse = false;
                         }
                     }
                     if (e.Node.Checked == true || NoFalse)
                     {
                         foreach (TreeNode tn in e.Node.Nodes)
                         {
                             if (tn.Checked != e.Node.Checked)
                             {
                                 tn.Checked = e.Node.Checked;
                             }
                         }
                     }
                 }
                 if (e.Node.Parent != null && e.Node.Parent is TreeNode)
                 {
                     bool ParentNode = true;
                     foreach (TreeNode tn in e.Node.Parent.Nodes)
                     {
                         if (tn.Checked == false)
                         {
                             ParentNode = false;
                         }
                     }
                     if (e.Node.Parent.Checked != ParentNode && (e.Node.Checked == false || e.Node.Checked == true && e.Node.Parent.Checked == false))
                     {
                         e.Node.Parent.Checked = ParentNode;
                     }
                 }
             }
             catch { }
             #endregion
         }
        #endregion

         private void Btn_Seleparete_Click(object sender, EventArgs e)
         {
             if (Btn_Seleparete.Text == "合并")
             {

                 Btn_Seleparete.Text = "分离";
                 this.TopLevel = false;
                 _panel.Controls.Add(this);
                 _panel.Refresh();
                 this.Move += MyUpdatePackageFileVerification_Move;
             }
             else
             {
                 flag = "btn";
                 MyUpdatePackageFileVerification_Move(sender, e);
             }
         }

         #region 逻辑使用到的自定义方法
         private void VerifiyIniExis()
        {
            //判断存在配置文件choose.ini吗 ，不存在创建choose.ini的默认的文件
           if(!inifilehelper.IsExistFile(chooseinipath))
           {
               //[titlename]------可选功能，显示于可选功能列表下
               //checktype=-------可选功能类型。
               //check1=-----------检查项1
               inifilehelper.CreateFile(chooseinipath, "");
           }
           //判断存在配置文件default.ini吗 ，不存在创建default.ini的默认的文件
           if (!inifilehelper.IsExistFile(defaultinipath))
           {
               inifilehelper.CreateFile(defaultinipath, "");
           }
        }

        /// <summary>
        /// 初始化功能列表配置菜单列表
        /// </summary>
         private void LoadFunctionList()
        {
            if( LoadChooseFunctionList().Count>0)BuildChooseCheckBoxList();
            if( LoadDefaultFunctionList().Count>0)BuildDefaultCheckBoxList();
        }

        /// <summary>
         /// 加载配置文件default.ini初始化默认功能列表,加载到树
        /// </summary>
         private Dictionary<string, List<string>> LoadDefaultFunctionList()
         {
             Dictionary<string, List<string>> defaultchecklist = new Dictionary<string, List<string>>();
             //遍历check节点的key-value值
             string[] titiledefaults = inifilehelper.ReadIniTitles(defaultinipath);
             int checkcount = titiledefaults.Length;
             if (checkcount > 0)
             {
                 for (int icount = 0; icount <= checkcount-1; icount++)
                 {
                     string checkvalue = titiledefaults[icount];
                     string[] childcheck = inifilehelper.ReadIniArray(checkvalue, defaultinipath);
                     int countcheckkey = childcheck.Length-2;
                     List<string> tchildcheck = new List<string>();
                     for(int i =1;i<= countcheckkey;i++)
                     {
                          tchildcheck.Add(inifilehelper.ReadIniValue(checkvalue, "check" + i, "", defaultinipath));
                     }

                     defaultchecklist.Add(checkvalue, tchildcheck);
                 }
             }
             return defaultchecklist;
         }

        /// <summary>
        /// 根据配置文件choose.ini生成界面功能列表-可选功能选择框
        /// </summary>
         private void BuildChooseCheckBoxList()
         {
             var chooseinichecklist = LoadChooseFunctionList();
             foreach(var choosecheckkeyvalue in chooseinichecklist)
             {
                 if(choosecheckkeyvalue.Value != "")
                 cBoxListFunction.Items.Add(" "+choosecheckkeyvalue.Value, false);
             }
         }

         /// <summary>
         /// 根据配置文件default.ini生成界面功能列表-可选功能选择框
         /// </summary>
         private void BuildDefaultCheckBoxList()
         {
             var defaultinichecklist = LoadDefaultFunctionList();
             foreach (var defaultcheckkeyvalue in defaultinichecklist)
             {
                 if (defaultcheckkeyvalue.Key != "") { 
                     //cBoxListDefaultFunction.Items.Add(" " + defaultcheckkeyvalue.Value, true);
                     TreeNode fNode=  tv_default.Nodes.Add(defaultcheckkeyvalue.Key);
                     fNode.Checked = true;
                     foreach(string childnode in defaultcheckkeyvalue.Value )
                     {
                         //string childnodefileinfofilename = childnode.
                         if (childnode.LastIndexOf('\\') > 0 && childnode.LastIndexOf('.') > 0)
                         {
                             string filternodetext = childnode.Substring(childnode.LastIndexOf('\\'), childnode.Length - childnode.LastIndexOf('\\'));
                             /*if (filternodetext.IndexOf("\\") > 0) {*/ filternodetext = filternodetext.Replace("\\","");
                             byte[] bytes = Encoding.Default.GetBytes(filternodetext);
                             if(bytes.Length >= 26)
                             {
                                 TreeNode lNode = new TreeNode(filternodetext);
                                 lNode.ToolTipText = childnode;
                                 lNode.Tag = childnode;
                                 fNode.Nodes.Add(lNode);
                                 fNode.Collapse(true);
                                 lNode.Checked = true;
                             }
                             else
                             {                       
                             TreeNode lNode = new TreeNode(filternodetext);
                             lNode.ToolTipText = childnode;
                             lNode.Tag = childnode;
                             fNode.Nodes.Add(lNode);
                             fNode.Expand();
                             lNode.Checked = true;
                             }
                         }
                         else {
                             byte[] bytes = Encoding.Default.GetBytes(childnode);
                             if (bytes.Length >= 26)
                             {
                                 fNode.Nodes.Add(new TreeNode(childnode) { Checked = true });
                                 fNode.Collapse();
                             }
                             else
                             {
                                 fNode.Nodes.Add(new TreeNode(childnode) { Checked = true });
                                 fNode.Expand();
                             }

                         }
                     }
                 }
             }
         }
        /// <summary>
         /// 加载配置文件choose.ini初始化默认功能列表
        /// </summary>
         private Dictionary<int,string> LoadChooseFunctionList()
         {
             Dictionary<int, string> choosechecklist = new Dictionary<int, string>();
             //遍历check节点的key-value值
             string[] titilechooses = inifilehelper.ReadIniTitles(chooseinipath);
            int checkcount = titilechooses.Length;
             if(checkcount>0)
             {
                 for(int icount =0;icount<=checkcount-1;icount++)
                 {
                     string checkvalue = titilechooses[icount];
                     choosechecklist.Add(icount, checkvalue);
                 }
             }
             return choosechecklist;
         }

         private List<string> getChooseIniCheckArrayByTitle(string title)
         {
             //遍历check节点的key-value值
             string checkvalue = title;
                     string[] childcheck = inifilehelper.ReadIniArray(checkvalue, chooseinipath);
                     int countcheckkey = childcheck.Length-2;
                     List<string> tchildcheck = new List<string>();
                     for(int i =1;i<= countcheckkey;i++)
                     {
                         tchildcheck.Add(inifilehelper.ReadIniValue(checkvalue, "check" + i, "", chooseinipath));
                     }
                     return tchildcheck;
         }

         private List<string> getChooseIniCheckArrayWithNoteByTitle(string title) 
         {
             //遍历check节点的key-value值
             string checkvalue = title;
             string[] childcheck = inifilehelper.ReadIniArray(checkvalue, chooseinipath);
             int countcheckkey = childcheck.Length - 2;
             List<string> tchildcheck = new List<string>();
             for (int i = 1; i <= countcheckkey; i++)
             {
                 tchildcheck.Add(inifilehelper.ReadIniValue(checkvalue, "check" + i, "", chooseinipath));
             }
             string note = inifilehelper.ReadIniValue(title, "note", "", chooseinipath);
             tchildcheck.Add(note);
             return tchildcheck;
         }
         private List<string> getDefaultIniCheckArrayByTitle(string title)
         {
             //遍历check节点的key-value值
             string checkvalue = title;
             string[] childcheck = inifilehelper.ReadIniArray(checkvalue, defaultinipath);
             int countcheckkey = childcheck.Length - 2;
             List<string> tchildcheck = new List<string>();
             for (int i = 1; i <= countcheckkey; i++)
             {
                 tchildcheck.Add(inifilehelper.ReadIniValue(checkvalue, "check" + i, "", defaultinipath));
             }
             return tchildcheck;
         }

         private List<string> getDefaultIniCheckArrayWithNoteByTitle(string title)
         {
             //遍历check节点的key-value值
             string checkvalue = title;
             string[] childcheck = inifilehelper.ReadIniArray(checkvalue, defaultinipath);
             int countcheckkey = childcheck.Length - 2;
             List<string> tchildcheck = new List<string>();
             for (int i = 1; i <= countcheckkey; i++)
             {
                 tchildcheck.Add(inifilehelper.ReadIniValue(checkvalue, "check" + i, "", defaultinipath));
             }
             string note = inifilehelper.ReadIniValue(title, "note", "", defaultinipath);
             tchildcheck.Add(note);
             return tchildcheck;
         }
  
        /// <summary>
        /// 递归遍历目录下的所有文件
        /// </summary>
        /// <param name="strBaseDir">文件夹路径</param>
        public void GetAllDirList(string strBaseDir)
        {
            DirectoryInfo di = new DirectoryInfo(strBaseDir);
            DirectoryInfo[] diA = di.GetDirectories();
            for (int i = 0; i < diA.Length; i++)
            {
                packagepath
                .Add(diA[i].FullName);
                GetAllDirList(diA[i].FullName);
            }

        }
        public void GetAllClientDirList(string strBaseDir)
        {
            if (strBaseDir.Length <= 0) { return; }
            DirectoryInfo di = new DirectoryInfo(strBaseDir);
            DirectoryInfo[] diA = di.GetDirectories();
            for (int i = 0; i < diA.Length; i++)
            {
                clientpath
                .Add(diA[i].FullName);
                GetAllClientDirList(diA[i].FullName);
            }
        }

        /// <summary>
        /// 获取更新包所有文件:
        /// 保存：packagefiles ： 存储选择更新包文件夹后解析的所有文件路径的集合
        /// </summary>
        /// <param name="foldname">更新包文件夹目录</param>
        private void getAllPackageFiles(string foldname)
        {
            packagepath.Clear();
            packagefiles.Clear();
            GetAllDirList(foldname);
            packagepath.Insert(0, foldname);
            //简化更新包文件路径，得到最后面的字符
            for (int i = 0; i <= packagepath.Count - 1; i++)
            {
                foreach (string ff in Directory.GetFiles(packagepath[i].ToString()))
                {
                    string ffext = ff.Replace(foldname, "");
                    ffext = ffext.Replace('\\', '/');
                    ffext = ffext.Substring(1, ffext.Length - 1);
                    packagefiles.Add(ffext);
                }
            }
        }

        /// <summary>
        /// 获取客户端全部的文件
        /// </summary>
        /// <param name="foldname"></param>
        private void getAllClientFiles(string foldname, out int filescount)
        {
            clientpath.Clear();
            clientfiles.Clear();
            GetAllClientDirList(foldname);
            clientpath.Insert(0, foldname);
            //简化更新包文件路径，得到最后面的字符
            for (int i = 0; i <= clientpath.Count - 1; i++)
            {
                foreach (string ff in Directory.GetFiles(clientpath[i].ToString()))
                {
                    string ffext = ff.Replace(foldname, "");
                    ffext = ffext.Replace('\\', '/');
                    ffext = ffext.Substring(1, ffext.Length - 1);
                    clientfiles.Add(ffext);
                }
            }
            filescount = clientfiles.Count;
        }

        private void aysnLoadClientview(string fold)
        {
            //int clientfilecount = 0;
            //BeginInvoke(new UpdateLabMesDelegate(UpdateLabMes), "正在载入客户端文件，分析文件夹文件数目...");
            //getAllClientFiles(fold, out clientfilecount);
            //BeginInvoke(new UpdateLabMesDelegate(UpdateLabMes), "正在载入客户端文件...");
            //BeginInvoke(new UpdateValueDelegate(UpdateValueField), clientfilecount);
            clientpath.Clear();
            clientfiles.Clear();
            GetAllClientDirList(fold);
            clientpath.Insert(0, fold);
            for (int i = 0; i <= clientpath.Count - 1; i++)
            {
                foreach (string ff in Directory.GetFiles(clientpath[i].ToString()))
                {
                    string ffext = ff.Replace(fold, "");
                    ffext = ffext.Replace('\\', '/');
                    ffext = ffext.Substring(1, ffext.Length - 1);
                    clientfiles.Add(ffext);
                    //Invoke(new UpdateTextFieldDelegate(UpdateTextField), clientfiles.Count.ToString() + "/" + clientfilecount.ToString() , clientfiles.Count);
                }
            }
        }

        private void BeginAysnLoadClient()
        {
            aysnLoadClientview(myclientselectfold);
            Invoke(new UpdateControlDelegate(UpdateControl));
            BeginInvoke(new UpdateClientLoadEndDelegate(UpdateClientLoadEnd));
            
        }

        /// <summary>
        /// 输出结果文件带样式：
        /// </summary>
        /// <param name="color"></param>
        /// <param name="familyfont"></param>
        /// <param name="fontsize"></param>
        /// <param name="context"></param>
        private void setWriteFontStyle(Color color, string familyfont, float fontsize, bool isbold, string context)
        {
            richTextBoxPrintWaring.SelectionColor = color;
            if (isbold == true) { richTextBoxPrintWaring.SelectionFont = new System.Drawing.Font(familyfont, fontsize, FontStyle.Bold); } else { richTextBoxPrintWaring.SelectionFont = new System.Drawing.Font(familyfont, fontsize, FontStyle.Regular); };
            richTextBoxPrintWaring.AppendText(context + "\n");
        }

        private void RarFile(string saverarpath, string needrarfilepath)
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinRAR.exe");
            //RegistryKey regkey = Registry.ClassesRoot.OpenSubKey(@"Applications\WinRAR.exe\shell\open\command");
            string strkey = regkey.GetValue("").ToString();
            regkey.Close();
            //return strkey.Substring(1, strkey.Length - 7);
            string winrarexefilepath = strkey;
            Process p = new Process();
            p.StartInfo.FileName = winrarexefilepath;
            p.StartInfo.Arguments = "a -as -s " + saverarpath + " " + needrarfilepath + " ";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.ErrorDialog = false;
            p.Start();
            int idx = 1;
            while (!p.HasExited)
            {
                idx++;
                p.WaitForExit(500);
                if (idx == 5)
                {
                    //p.Kill();
                }
            }
            p.Close();
            p.Dispose();

        }

        private CHOOSEFUNCTIONKEY ReadChooseIniConfig(string inititle)
        {
            //读取可选功能配置的checktype
            int chooseinichecktype = int.Parse(inifilehelper.ReadIniValue(inititle, "checktype", "0", chooseinipath));
            switch (chooseinichecktype)
            {
                case 1:
                    return CHOOSEFUNCTIONKEY._CHECKTYPE1;
                case 2:
                    return CHOOSEFUNCTIONKEY._CHECKTYPE2;
                case 3:
                    return CHOOSEFUNCTIONKEY._CHECKTYPE3;
                default:
                    return CHOOSEFUNCTIONKEY._NOCHECKTYPE;
            }
        }

        private DEFAULTFUNCTIONKEY ReadDefaultIniConfig(string inititle)
        {
            //读取可选功能配置的checktype
            int defaultinichecktype = int.Parse(inifilehelper.ReadIniValue(inititle, "checktype", "0", defaultinipath));
            switch (defaultinichecktype)
            {
                case 1:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE1;
                case 2:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE2;
                case 3:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE3;
                case 4:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE4;
                case 5:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE5;
                case 6:
                    return DEFAULTFUNCTIONKEY._CHECKTYPE6;
                default:
                    return DEFAULTFUNCTIONKEY._NOCHECKTYPE;
            }
        }

       private void LoadClientFilesIfIniHasConf()
       {
           try { 
           List<string> defaultinicontext4= getDefaultIniCheckArrayWithNoteByTitle("文件修改时间");
           string defaultinicheck4clientfile = defaultinicontext4[0];
           if (Directory.Exists(defaultinicheck4clientfile))
           {
               Invoke(new UpdateClientTextDelegate(UpdateClientText), defaultinicheck4clientfile);
               myclientselectfold = defaultinicheck4clientfile;
               if (defaultinicheck4clientfile.EndsWith("\\"))
               {
                   defaultinicheck4clientfile = defaultinicheck4clientfile.Substring(0, defaultinicheck4clientfile.Length - 1);
               }
               //改进
               clientpath.Clear();
               clientfiles.Clear();
               GetAllClientDirList(defaultinicheck4clientfile);
               clientpath.Insert(0, defaultinicheck4clientfile);
               for (int i = 0; i <= clientpath.Count - 1; i++)
               {
                   foreach (string ff in Directory.GetFiles(clientpath[i].ToString()))
                   {
                       string ffext = ff.Replace(defaultinicheck4clientfile, "");
                       ffext = ffext.Replace('\\', '/');
                       ffext = ffext.Substring(1, ffext.Length - 1);
                       clientfiles.Add(ffext);
                       //Invoke(new UpdateTextFieldDelegate(UpdateTextField), clientfiles.Count.ToString() + "/" + clientfilecount.ToString(), clientfiles.Count);
                   }
               }
               //00
               //getAllClientFiles(defaultinicheck4clientfile,out curfilecount);
               Invoke(new UpdateControlDelegate(UpdateControl));
           }
           else {
               Invoke(new UpdateClientTextDelegate(UpdateClientText), "配置文件夹的路径在硬盘找不到");
               //labClientPath.Text = "配置文件夹的路径在硬盘找不到";
           }
           }
           catch(Exception ex){
               MessageBox.Show("读取配置文件出错：\n  "+ex.Message,"提示");
           }
           finally
           {
               clientBeginLoad.Abort();
           }
       }
       private void aysnBeginShowProgress()
       {
           Invoke(new UpdatePackageBeginDelegate(UpdatePackageBegin));
           //初始化更新包的路径和文件信息
           getAllPackageFiles(packageselectfold);
           Invoke(new UpdatePackageEndDelegate(UpdatePackageEnd));
       }
        #endregion

        private void btnLoadPackage_Click(object sender, EventArgs e)
        {
            scbLoadPackageOk.Checked = false;
            scbLoadPackageOk.Text = "";
            scbLoadPackageOk.Visible = false;
            btnLoadPackage.ForeColor = Color.Black;
            folderBrowserDialogPackage.ShowDialog();
            string foldname = folderBrowserDialogPackage.SelectedPath;
            packageselectfold = foldname;
            if (foldname == "") { MessageBox.Show("你取消了更新包检查！", "提示"); return; }
            labPackagePath.Text = foldname == "更新包路径..." ? "" : foldname;
            if (labPackagePath.Text.Length >= 25) {
                labPackagePathReal.Text = labPackagePath.Text.Substring(0, 25) + "...";
            }
            else { 
            labPackagePathReal.Text = labPackagePath.Text;
            }
            packageBeginLoad = new Thread(new ThreadStart(aysnBeginShowProgress));
            packageBeginLoad.Start();
        }

        #region 逻辑使用到的委托
        delegate void UpdateTextFieldDelegate(String newText,int curcount);
        delegate void UpdateValueDelegate(int num);
        delegate void UpdateLabMesDelegate(string mes);
        delegate void UpdateControlDelegate();
        delegate void UpdateClientTextDelegate(string mes);
        delegate void UpdateClientLoadEndDelegate();
        delegate void UpdatePackageBeginDelegate();
        delegate void UpdatePackageEndDelegate();
        private void UpdateClientText(string mes)
        {
            labClientPath.Text = mes;
        }
        private void UpdateLabMes(string mes)
        {
            label4.Text = mes;
        }
        private void UpdateTextField(String someText, int curcount)
        {
            lbFilesCount.Text = someText;
            skinProgressBar1.Value = curcount;
        }
        private void UpdateValueField(int num)
        {

            skinProgressBar1.Maximum = num;

        }
        private void UpdateControl()
        {
            scbLoadClientOk.Checked = true;
            scbLoadClientOk.Text = "载入成功";
            btnLoadClient.ForeColor = Color.FromArgb(0, 51, 161, 224);
            scbLoadClientOk.Visible = true;
        }
        private void UpdateClientLoadEnd()
        {
            panelLoadProcess.Visible = false;
            btnLoadClient.Enabled = true;
            labClientPath.Text = myclientselectfold == "客户端路径..." ? "" : myclientselectfold;
        }
        private void UpdatePackageBegin()
        {
            panelLoadProcess.Visible = true;
            btnLoadPackage.Enabled = false;
        }
        private void UpdatePackageEnd()
        {
            panelLoadProcess.Visible = false;
            btnLoadPackage.Enabled = true;
            btnLoadPackage.ForeColor = Color.FromArgb(0, 51, 161, 224);
            scbLoadPackageOk.Checked = true;
            scbLoadPackageOk.Text = "载入成功";
            scbLoadPackageOk.Visible = true;
        }
        #endregion
     
        private void btnLoadClient_Click(object sender, EventArgs e)
        {
            scbLoadClientOk.Checked = false;
            scbLoadClientOk.Text = "";
            scbLoadClientOk.Visible = false;
            btnLoadClient.ForeColor = Color.Black;
            folderBrowserDialogClient.ShowDialog();
            string foldname = folderBrowserDialogClient.SelectedPath;
            if(foldname =="")
            {
                MessageBox.Show("你取消了客户端选择!","提示");
                return;
            }
            myclientselectfold = foldname;
            clientLoad = new Thread(new ThreadStart(BeginAysnLoadClient));
            panelLoadProcess.Visible = true;
            btnLoadClient.Enabled = false;
            clientLoad.Start();
        }

        /// <summary>
        /// 执行检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRunVerity_Click(object sender, EventArgs e)
        {
            string errselectmes = "";
            //检查出来的false数量
            int falsenub = 0;
            //结果分析出来的list信息
            List<string> especiallyinfos = new List<string>();
            if(packageselectfold.Trim().Length<=0 )
            {
                errselectmes+="你没有选择更新包\n";
            }
            if (myclientselectfold.Trim().Length <= 0)
            {
                errselectmes += "你没有选择客户端!";
            }
            if (errselectmes.Trim().Length > 0)
            {
                MessageBox.Show(errselectmes, "提示");
                return;
            }
            richTextBoxPrintWaring.Text = "";
            setWriteFontStyle(Color.Gray,"微软雅黑",14f,true,"具体内容:");
            #region 可选功能检查
            foreach (string title in cBoxListFunction.CheckedItems)
            {
                CHOOSEFUNCTIONKEY _cfk = ReadChooseIniConfig(title);
                switch (_cfk)
                {
                    case CHOOSEFUNCTIONKEY._CHECKTYPE1:
                        setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                        var chooseinicontext = /*FilterNullChooseIniList().Values;*/new List<string>();
                        chooseinicontext = getChooseIniCheckArrayByTitle(title);
                        var packagefilelist = packagefiles;
                        foreach (var sVerityStr in chooseinicontext)
                        {
                            if (packagefilelist.FindAll(p => (p.Replace('/', '\\')).Contains(sVerityStr)).Count > 0)
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Green;
                                richTextBoxPrintWaring.AppendText(sVerityStr + "----true\n");
                            }
                            else
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Red;
                                richTextBoxPrintWaring.AppendText(sVerityStr + "----false\n");
                                falsenub += 1;
                            }
                        }
                        break;
                    case CHOOSEFUNCTIONKEY._CHECKTYPE2:
                        setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                        richTextBoxPrintWaring.SelectionColor = Color.Black;
                        richTextBoxPrintWaring.SelectionFont = new System.Drawing.Font("微软雅黑", 17.5f);
                        //packagepath : 更新包的所有子文件夹路径    
                        var packagefoldlist = new List<string>();
                        foreach (var apath in packagepath.ToArray())
                        {
                            string ffext = apath.ToString().Replace(packageselectfold, "");
                            if (ffext != "")
                            {
                                ffext = ffext.Replace('\\', '/');
                                ffext = ffext.Substring(1, ffext.Length - 1);
                                packagefoldlist.Add(ffext.ToString());
                            }
                        }
                        foreach (var sVerityStrPath in getChooseIniCheckArrayByTitle(title))
                        {
                            if (packagefoldlist.FindAll(path => path.Contains(sVerityStrPath)).Count > 0)
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Green;
                                richTextBoxPrintWaring.AppendText(sVerityStrPath + "----true\n");
                            }
                            else
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Red;
                                richTextBoxPrintWaring.AppendText(sVerityStrPath + "----false\n");
                                falsenub += 1;
                            }
                        }
                        break;
                    case CHOOSEFUNCTIONKEY._CHECKTYPE3:
                        setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                        List<string> chooseinicontext3 = new List<string>();
                        chooseinicontext3 = getChooseIniCheckArrayWithNoteByTitle(title);
                        var packagefilelist3 = packagefiles;
                        string note = chooseinicontext3[chooseinicontext3.Count - 1].ToString();
                        chooseinicontext3.RemoveAt(chooseinicontext3.Count - 1);
                        foreach (var sVerityStr in chooseinicontext3)
                        {
                            if (packagefilelist3.FindAll(p => (p.Replace('/', '\\')).Contains(sVerityStr)).Count > 0)
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Green;
                                richTextBoxPrintWaring.AppendText(note + "\n");
                                string[] fileshortnamearray = sVerityStr.Split('\\');
                                especiallyinfos.Add(fileshortnamearray[fileshortnamearray.Length-1]+" "+ note);
                            }
                            else
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Red;
                                richTextBoxPrintWaring.AppendText(sVerityStr + "----false\n");
                                falsenub += 1;
                            }
                        }

                        #region old verion 1.0 注释
                        /*
		                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                        richTextBoxPrintWaring.SelectionColor = Color.Black;
                        richTextBoxPrintWaring.SelectionFont = new System.Drawing.Font("微软雅黑", 17.5f);
                        int count3 = 0;
                        var chooseinicontext3 = new Dictionary<int, string>();
                        foreach (var check in cBoxListFunction.CheckedItems)
                        {
                            count3++;
                            chooseinicontext3.Add(count3, (string)check);
                        }
                        var packagefilelist3 = packagefiles;
                        foreach (var sVerityStr in chooseinicontext3)
                        {
                            if (packagefilelist3.FindAll(p => p.Contains(sVerityStr.Value)).Count > 0)
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Green;
                                richTextBoxPrintWaring.AppendText(_choosenote + "\n");
                            }
                            else
                            {
                                richTextBoxPrintWaring.SelectionColor = Color.Red;
                                richTextBoxPrintWaring.AppendText(sVerityStr.Value + "----false\n");
                            }
                        } */
                        #endregion
                        break;
                    case CHOOSEFUNCTIONKEY._NOCHECKTYPE:
                        MessageBox.Show("checktype=0，不检查。", "提示");
                        break;
                }
            } 
            #endregion

            #region 默认功能检查
            foreach(TreeNode tnChild in tv_default.Nodes)
            {
                if(tnChild.Level == 0)
                {
                    if(tnChild.Checked)
                    {
                        string title = tnChild.Text;
                        DEFAULTFUNCTIONKEY _cfk = ReadDefaultIniConfig(title);
                        switch (_cfk)
                        {
                            case DEFAULTFUNCTIONKEY._CHECKTYPE1:
                                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                var defaultinicontext = /*FilterNullChooseIniList().Values;*/new List<string>();
                                defaultinicontext = getDefaultIniCheckArrayByTitle(title);
                                var packagefilelist = packagefiles;
                                foreach (var sVerityStr in defaultinicontext)
                                {
                                    if (packagefilelist.FindAll(p => (p.Replace('/', '\\')).Contains(sVerityStr)).Count > 0)
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Green;
                                        richTextBoxPrintWaring.AppendText(sVerityStr + "----true\n");
                                    }
                                    else
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Red;
                                        richTextBoxPrintWaring.AppendText(sVerityStr + "----false\n");
                                        falsenub += 1;
                                    }
                                }
                                break;
                            case DEFAULTFUNCTIONKEY._CHECKTYPE2:
                                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                richTextBoxPrintWaring.SelectionColor = Color.Black;
                                richTextBoxPrintWaring.SelectionFont = new System.Drawing.Font("微软雅黑", 17.5f);
                                //packagepath : 更新包的所有子文件夹路径    
                                var packagefoldlist = new List<string>();
                                foreach (var apath in packagepath.ToArray())
                                {
                                    string ffext = apath.ToString().Replace(packageselectfold, "");
                                    if (ffext != "")
                                    {
                                        ffext = ffext.Replace('\\', '/');
                                        ffext = ffext.Substring(1, ffext.Length - 1);
                                        packagefoldlist.Add(ffext.ToString());
                                    }
                                }
                                foreach (var sVerityStrPath in getDefaultIniCheckArrayByTitle(title))
                                {
                                    if (packagefoldlist.FindAll(path => path.Contains(sVerityStrPath)).Count > 0)
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Green;
                                        richTextBoxPrintWaring.AppendText(sVerityStrPath + "----true\n");
                                    }
                                    else
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Red;
                                        richTextBoxPrintWaring.AppendText(sVerityStrPath + "----false\n");
                                        falsenub += 1;
                                    }
                                }
                                break;
                            case DEFAULTFUNCTIONKEY._CHECKTYPE3:
                                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                List<string> defaultinicontext3 = new List<string>();
                                defaultinicontext3 = getDefaultIniCheckArrayWithNoteByTitle(title);
                                var packagefilelist3 = packagefiles;
                                string note = defaultinicontext3[defaultinicontext3.Count - 1].ToString();
                                defaultinicontext3.RemoveAt(defaultinicontext3.Count - 1);
                                foreach (var sVerityStr in defaultinicontext3)
                                {
                                    if (packagefilelist3.FindAll(p => (p.Replace('/', '\\')).Contains(sVerityStr)).Count > 0)
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Green;
                                        richTextBoxPrintWaring.AppendText(note + "\n");
                                        string[] fileshortnamearray = sVerityStr.Split('\\');
                                        especiallyinfos.Add(fileshortnamearray[fileshortnamearray.Length - 1] + " " + note);
                                    }
                                    else
                                    {
                                        richTextBoxPrintWaring.SelectionColor = Color.Red;
                                        richTextBoxPrintWaring.AppendText(sVerityStr + "----false\n");
                                        falsenub += 1;
                                    }
                                }
                                break;
                            case DEFAULTFUNCTIONKEY._CHECKTYPE4:
                                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                string checktype4mes = "";
                                int haserr = 0;
                                 List<string> defaultinicontext4= getDefaultIniCheckArrayWithNoteByTitle(title);
                                 string note4 = defaultinicontext4[defaultinicontext4.Count - 1].ToString();
                                //更新包的文件列表
                                List<string> packagefileforchecktype4 = packagefiles;
                                packagefileforchecktype4 = packagefileforchecktype4.FindAll(selectwheremyisclientfiles => selectwheremyisclientfiles.Contains("客户端更新"));
                                List<string> packageclientfiles = new List<string>();
                                foreach(string cur in packagefileforchecktype4)
                                {
                                    string strcur = cur.Replace("客户端更新/", "");
                                    packageclientfiles.Add(strcur);
                                }
                                //客户端的文件列表
                                List<string> clientfilesforchecktype4 = clientfiles;
                                foreach (string packagefilecur in packageclientfiles)
                                {
                                    string checkfilecur = packagefilecur.Replace("/", "\\");
                                    checkfilecur = packageselectfold + "\\客户端更新\\" + checkfilecur;
                                    FileInfo checkfilecurinfo = new FileInfo(checkfilecur);
                                    DateTime checkfilecurinfolastupdatetime = checkfilecurinfo.LastWriteTime;
                                    //clientfilesforchecktype4.fi
                                    string clientfindcur = clientfilesforchecktype4.Find(f=>f.Equals(packagefilecur));
                                    DateTime checkclientfilecurinfolastupdatetime = DateTime.MinValue;
                                    if(clientfindcur != null)
                                    {
                                        if (clientfindcur.Contains("/")) { clientfindcur = clientfindcur.Replace("/", "\\"); }
                                        FileInfo curclientfilecurinfo = new FileInfo(myclientselectfold + "\\" + clientfindcur);
                                        checkclientfilecurinfolastupdatetime = curclientfilecurinfo.LastWriteTime;
                                    }
                                    if(checkfilecurinfolastupdatetime < checkclientfilecurinfolastupdatetime)
                                    {
                                        haserr += 1;
                                        checktype4mes += "     "+haserr.ToString()+": "+packagefilecur + " " + note4 + "\n";
                                    }
                                }
                                if(haserr > 0)
                                {
                                    richTextBoxPrintWaring.SelectionColor = Color.Green;
                                    richTextBoxPrintWaring.AppendText( note4+"详情文件如下:\n" );
                                    setWriteFontStyle(Color.LightSeaGreen, "微软雅黑", 13.5f, false, checktype4mes);
                                    especiallyinfos.Add(note4 + ",请看详情！");
                                }
                                break;
                            case DEFAULTFUNCTIONKEY._CHECKTYPE5:
                                setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                //读取check
                                List<string> check5value = getDefaultIniCheckArrayWithNoteByTitle(title);
                                string note5 = check5value[check5value.Count - 1].ToString();
                                //只可能有一项check
                                int check5valuenub = int.Parse( check5value[0].Trim().ToString());
                                string packagevertionnum = packageselectfold.Substring(packageselectfold.Length - check5valuenub , check5valuenub);
                                //客户端的文件列表
                                List<string> clientfilesforchecktype5 = clientfiles;
                                string clientfindcurver = clientfilesforchecktype5.Find(f => f.Equals("version.dat"));
                                if (clientfindcurver != null)
                                    {
                                        if (clientfindcurver.Contains("/")) { clientfindcurver = clientfindcurver.Replace("/", "\\"); }
                                        string clientverstr =  inifilehelper.ReadAllText(myclientselectfold + "\\" + clientfindcurver);
                                        //clientverstr = clientverstr.Replace("\\", "/");
                                        //clientverstr = clientverstr.Replace("n", "");
                                        //clientverstr = clientverstr.Replace("r", "");
                                        clientverstr = clientverstr.Trim();
                                        if(clientverstr != packagevertionnum)
                                        {
                                            richTextBoxPrintWaring.SelectionColor = Color.Green;
                                            richTextBoxPrintWaring.AppendText(note5 + "\n");
                                            especiallyinfos.Add(note5 );
                                        }
                                    }
                                break;
                            case DEFAULTFUNCTIONKEY._CHECKTYPE6:
                                 setWriteFontStyle(Color.Purple, "微软雅黑", 16.5f, false, "[" + title + "]");
                                 //读取check
                                 List<string> check6value = getDefaultIniCheckArrayWithNoteByTitle(title);
                                 string note6 = check6value[check6value.Count - 1].ToString();
                                  //获取临时目录路径
                                 string temp = Environment.GetEnvironmentVariable("TEMP");
                                 //DirectoryInfo info = new DirectoryInfo(temp);
                                 //压缩更新包到系统临时目录
                                 string saverarpath = temp + "\\" + Guid.NewGuid() + ".rar";
                                 RarFile(saverarpath, packageselectfold);
                                 FileInfo packagerarinfo = new FileInfo(saverarpath);
                                // 压缩后的文件大小，单位=M
                                 long filelen = packagerarinfo.Length / 1024/1024;
                                //INI约束的更新包大小
                                 long constraintrarlen = long.Parse(check6value[0].Trim());
                                 if (filelen > constraintrarlen)
                                 {
                                     richTextBoxPrintWaring.SelectionColor = Color.Green;
                                     richTextBoxPrintWaring.AppendText(note6 + "\n");
                                     especiallyinfos.Add(note6);
                                 }
                                break;
                            case DEFAULTFUNCTIONKEY._NOCHECKTYPE:
                                MessageBox.Show("checktype=0，不检查。", "提示");
                                break;
                        }
                    }
                }
            }
            
            #endregion

            #region 最后统计汇总信息
            setWriteFontStyle(Color.Black, "微软雅黑", 14f, true, "" + "\n结果分析" + "");
            setWriteFontStyle(Color.Black, "微软雅黑", 13.5f, true, "有" + falsenub + "处FALSE,请注意查看。");
            especiallyinfos.ForEach((s) => setWriteFontStyle(Color.Black, "微软雅黑", 13.5f, true, s));
            #endregion

            richTextBoxPrintWaring.ScrollToCaret(); 
        }  

        private void btnLoadpCancel_Click(object sender, EventArgs e)
        {
           if(clientLoad.ThreadState == System.Threading.ThreadState.Running || clientLoad.IsAlive)
           {
               clientLoad.Abort();
               panelLoadProcess.Visible = false;
               btnLoadClient.Enabled = true;
           }
        }
    }
}
