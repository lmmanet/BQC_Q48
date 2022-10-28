using BQJX.Common;
using BQJX.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.ViewModels.Base;
using Q_Platform.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Windows
{
    public class AddSampleWinViewModel : MyViewModelBase
    {

        #region Properties
        public ObservableCollection<TechParamsModel> TechList { get; set; }

        public SampleModel SampleModel { get; set; }

        public List<SampleModel> SampleList { get; set; }

        /// <summary>
        /// 选中的工艺
        /// </summary>
        public TechParamsModel TechParamsModelSelected { get; set; }

        #endregion

        #region Commands

        public ICommand CancelCommand { get; set; }

        public ICommand WindowCloseCommand { get; set; }

        public ICommand DoneCommand { get; set; }


        /// <summary>
        /// 批量导入样品
        /// </summary>
        public ICommand ImportSampleCommand { get; set; }

        /// <summary>
        /// 工艺选择变更
        /// </summary>

        public ICommand ComboxSelectChangedCommand { get; set; }


        #endregion

        #region Construtors

        public AddSampleWinViewModel()
        {

            CancelCommand = new RelayCommand<object>(Cancel);
            WindowCloseCommand = new RelayCommand<object>(CloseWin);
            DoneCommand = new RelayCommand<object>(Done);
            ImportSampleCommand = new RelayCommand(ImportSample);
            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            GenerMock();
        }


        #endregion

        #region Private Methods

        private void ComboxSelectChanged(object obj)
        {
            var tech = obj as TechParamsModel;
            if (tech != null)
            {
                TechParamsModelSelected = tech;
            }
        }

        private void ImportSample()
        {

            //throw new NotImplementedException();
        }

        private void Cancel(object o)
        {
            var win = o as AddSampleWin;
            if (win != null)
            {
                win.DialogResult = false;
                win.Close();
            }

        }

        private void CloseWin(object o)
        {
            var win = o as AddSampleWin;
            if (win != null)
            {
                win.DialogResult = false;
                win.Close();
            }
        }

        private void Done(object o)
        {
            //参数检查
            if (!CheckParam())
            {
                return;
            }

            //转换模型
            var SampleModel = GenerateSampleModel();

            //发送工艺数据到主窗口
            Messenger.Default.Send<SampleModel>(SampleModel, "AddSampleModel");

            //关闭当前窗口，释放资源 
            var win = o as AddSampleWin;
            if (win != null)
            {
                win.DialogResult = true;
                win.Close();
            }
        }


        private bool CheckParam()
        {
            ////判断工艺名不能为空
            //if (string.IsNullOrEmpty(TechParamsModel.Name))
            //{
            //    MessageBox.Show("工艺名称不能为空");
            //    return false;
            //}

            ////总体加液量不能超过50
            //if ((TechParamsModel.Solvent_A + TechParamsModel.Solvent_B + TechParamsModel.Solvent_C + TechParamsModel.Solvent_D) >= 40)
            //{
            //    MessageBox.Show("总的加液量不能超过40ml");
            //    return false;
            //}

            ////加均质量不能超过20g
            //if (TechParamsModel.Junzhizi >= 20)
            //{
            //    MessageBox.Show("总的加均质子重量不能超过20g");
            //    return false;
            //}


            return true;
        }


        private List<SampleModel> GenerateSampleModelList()
        {




            return null;
        }


        /// <summary>
        /// 生成单个样品模型
        /// </summary>
        /// <returns></returns>
        private SampleModel GenerateSampleModel()
        {
            return new SampleModel()
            {
                Name1 = SampleModel.Name1,
                Name2 = SampleModel.Name2,
                SnNum1 = SampleModel.SnNum1,
                SnNum2 = SampleModel.SnNum2,
                TechParams = TechParamsModelTransferToTechParams(),
                CreateTime = DateTime.Now,
                TechName = TechParamsModelSelected.Name,
                StartMainStep = GetSampleStartMainStep(),
            };
        }

        /// <summary>
        /// 转换工艺参数
        /// </summary>
        /// <returns></returns>
        private TechParams TechParamsModelTransferToTechParams()
        {
            if (TechParamsModelSelected.Name == "Agilent 多兽药残留")
            {
                return new TechParams
                {
                    AddWater = TechParamsModelSelected.AddWater,
                    Solvent_A = TechParamsModelSelected.ACE,    //ACE
                    Solvent_B = TechParamsModelSelected.Acid,    //乙腈醋酸
                    Solvent_C = TechParamsModelSelected.Formic,    //甲酸乙腈溶液
                    WetTime = 0,
                    AddHomo = new double[3] { TechParamsModelSelected.Homo, 0, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, 0 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime,
                        TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationOneVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel,
                        TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VibrationTwoTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationTwoVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VortexTime = new int[] { TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime },
                    VortexVel = new int[] { TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel },
                    CentrifugalOneTime = new int[] { TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime },
                    CentrifugalOneVelocity = new int[] { TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel },
                    ExtractVolume = TechParamsModelSelected.ExtractVolume,
                    cusuanan = 5,                           //醋酸铵
                    Extract = 10,                           //全部倒入量
                    ConcentrationVolume = 2,                //浓缩量
                    ConcentrationTime = TechParamsModelSelected.ConcentrationTime,                  //
                    ConcentrationVel = TechParamsModelSelected.ConcentrationVel,
                    Redissolve = 0.95,                             //乙腈水溶液
                    Add_Mark_A = 50,                            //加标50uL                           
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x3DFDE1AB,                         //工艺  3DFD E1AB
                };
            }

            else if (TechParamsModelSelected.Name == "GB23200.113-2018 果蔬")
            {
                return new TechParams()
                {
                    AddWater = TechParamsModelSelected.AddWater,
                    Solvent_A = TechParamsModelSelected.ACE,    //ACE
                    Solvent_B = TechParamsModelSelected.Acid,    //乙腈醋酸
                    Solvent_C = TechParamsModelSelected.Formic,    //甲酸乙腈溶液
                    WetTime = 0,
                    AddHomo = new double[3] { 0, 0, TechParamsModelSelected.Homo },    //均质子
                    Solid_B = new double[3] { 0, 0, TechParamsModelSelected.MgSO4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, TechParamsModelSelected.NaCl },    //氯化钠
                    Solid_D = new double[3] { 0, 0, TechParamsModelSelected.Trisodium },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, TechParamsModelSelected.Monosodium },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime,
                        TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationOneVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel,
                        TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VibrationTwoTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationTwoVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VortexTime = new int[] { TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime },
                    VortexVel = new int[] { TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel },
                    CentrifugalOneTime = new int[] { TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime },
                    CentrifugalOneVelocity = new int[] { TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel },
                    ExtractVolume = TechParamsModelSelected.ExtractVolume,
                    ConcentrationVolume = 2,
                    ConcentrationTime = TechParamsModelSelected.ConcentrationTime,
                    ConcentrationVel = TechParamsModelSelected.ConcentrationVel,
                    Redissolve = 2,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml
                                                                 //移液高度 取液大管148
                                                                 //吐液小管     71.92
                                                                 //取液小管   44.5（用不上）
                                                                 //净化管取液 167
                                                                 //西林瓶取液  145.5
                                                                 //移栽大管取液 73.371
                                                                 //样品小瓶吐液 120
                                                                 //西林瓶吐液  105.085
                    Tech = 0xF03FE00,                         //工艺

                };
            }

            else if (TechParamsModelSelected.Name == "GB23200.113-2018 坚果、油料")
            {
                return new TechParams()
                {
                    AddWater = TechParamsModelSelected.AddWater,
                    Solvent_A = TechParamsModelSelected.ACE,    //ACE
                    Solvent_B = TechParamsModelSelected.Acid,    //乙腈醋酸
                    Solvent_C = TechParamsModelSelected.Formic,    //甲酸乙腈溶液
                    WetTime = 30,
                    AddHomo = new double[3] { 0, 0, TechParamsModelSelected.Homo },    //均质子
                    Solid_B = new double[3] { 0, 0, TechParamsModelSelected.MgSO4 },    //硫酸钠
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, TechParamsModelSelected.Sodium },    //乙酸钠
                    VibrationOneTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime,
                        TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationOneVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel,
                        TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VibrationTwoTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationTwoVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VortexTime = new int[] { TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime },
                    VortexVel = new int[] { TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel },
                    CentrifugalOneTime = new int[] { TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime },
                    CentrifugalOneVelocity = new int[] { TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel },
                    ExtractVolume = TechParamsModelSelected.ExtractVolume,
                    ConcentrationVolume = 2,
                    ConcentrationTime = TechParamsModelSelected.ConcentrationTime,
                    ConcentrationVel = TechParamsModelSelected.ConcentrationVel,
                    Redissolve = 2,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0xF03EE19,                         //工艺

                };
            }

            else if (TechParamsModelSelected.Name == "GB23200.121-2021 果蔬")
            {
                return new TechParams()
                {
                    AddWater = TechParamsModelSelected.AddWater,
                    Solvent_A = TechParamsModelSelected.ACE,    //ACE
                    Solvent_B = TechParamsModelSelected.Acid,    //乙腈醋酸
                    Solvent_C = TechParamsModelSelected.Formic,    //甲酸乙腈溶液
                    WetTime = 30,
                    AddHomo = new double[3] { 0, TechParamsModelSelected.Homo, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, TechParamsModelSelected.MgSO4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, TechParamsModelSelected.NaCl },    //氯化钠
                    Solid_D = new double[3] { 0, 0, TechParamsModelSelected.Trisodium },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, TechParamsModelSelected.Monosodium },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime,
                        TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationOneVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel,
                        TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VibrationTwoTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationTwoVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VortexTime = new int[] { TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime },
                    VortexVel = new int[] { TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel },
                    CentrifugalOneTime = new int[] { TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime },
                    CentrifugalOneVelocity = new int[] { TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel },
                    ExtractVolume = TechParamsModelSelected.ExtractVolume,
                    ConcentrationVolume = 0,
                    ConcentrationTime = 0,
                    ConcentrationVel = 0,
                    Redissolve = 0,                             //乙酸乙酯
                    Add_Mark_B = 0,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x801F4E0,                         //工艺

                };
            }

            else if (TechParamsModelSelected.Name == "GB23200.121-2021 坚果、油料")
            {
                return new TechParams()
                {
                    AddWater = TechParamsModelSelected.AddWater,
                    Solvent_A = TechParamsModelSelected.ACE,    //ACE
                    Solvent_B = TechParamsModelSelected.Acid,    //乙腈醋酸
                    Solvent_C = TechParamsModelSelected.Formic,    //甲酸乙腈溶液
                    WetTime = 30,
                    AddHomo = new double[3] { 0, TechParamsModelSelected.Homo, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, TechParamsModelSelected.MgSO4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, TechParamsModelSelected.Sodium },    //
                    VibrationOneTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime,
                        TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationOneVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel,
                        TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VibrationTwoTime = new int[] { TechParamsModelSelected.VibrationTime, TechParamsModelSelected.VibrationTime },
                    VibrationTwoVel = new int[] { TechParamsModelSelected.VibrationVel, TechParamsModelSelected.VibrationVel },
                    VortexTime = new int[] { TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime, TechParamsModelSelected.VortexTime },
                    VortexVel = new int[] { TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel, TechParamsModelSelected.VortexVel },
                    CentrifugalOneTime = new int[] { TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime, TechParamsModelSelected.CentrifugalTime },
                    CentrifugalOneVelocity = new int[] { TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel, TechParamsModelSelected.CentrifugalVel },
                    ExtractVolume = TechParamsModelSelected.ExtractVolume,
                    ConcentrationVolume = 0,
                    ConcentrationTime = 0,
                    ConcentrationVel = 0,
                    Redissolve = 0,                             //乙酸乙酯
                    Add_Mark_B = 0,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x801ECF9,                         //工艺

                };
            }

            else
            {
                return null;
            }
        }

        /// <summary>
        /// 样品起始步骤
        /// </summary>
        /// <returns></returns>
        private int GetSampleStartMainStep()
        {
            if (TechParamsModelSelected.Name == "GB23200.113-2018 果蔬")
            {
                return 3;
            }

            if (TechParamsModelSelected.Name == "GB23200.121-2021 果蔬" && TechParamsModelSelected.AddWater == 0)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }



        #endregion




        private void GenerMock()
        {
            TechList = new ObservableCollection<TechParamsModel>();

            TechList.Add(new TechParamsModel()
            {
                Name = "GB23200.113-2018 果蔬",

                AddWater = 0,                  //纯水
                ACE = 10,                      //乙腈
                Acid = 0,                      //醋酸
                Formic = 0,                    //甲酸
                Homo = 2,                      //均质子
                MgSO4 = 4,                     //硫酸镁，硫酸钠
                NaCl = 1,                      //氯化钠
                Trisodium = 1,                  //柠檬酸钠，二水合物
                Monosodium = 0.5,              //氢二钠，盐倍半水
                Sodium = 0,                    //乙酸钠
                VortexTime = 60,               //涡旋时间
                VortexVel = 2000,              //涡旋速度
                VibrationTime = 60,            //振荡时间
                VibrationVel = 420,            //振荡速度
                CentrifugalTime = 5,           //离心时间
                CentrifugalVel = 4200,         //离心速度
                ExtractVolume = 6,             //上清液提取量
                ConcentrationTime = 5,         //浓缩时间
                ConcentrationVel = 10000,      //浓缩速度
                Tech = 0xF03EE00,              //工艺
            });

            TechList.Add(new TechParamsModel()
            {
                Name = "GB23200.113-2018 坚果、油料",

                AddWater = 10,                 //纯水
                ACE = 0,                      //乙腈
                Acid = 15,                      //醋酸
                Formic = 0,                    //甲酸
                Homo = 1,                      //均质子
                MgSO4 = 0,                     //硫酸镁
                NaCl = 6,                      //氯化钠 /硫酸钠
                Trisodium = 0,                  //柠檬酸钠，二水合物
                Monosodium = 0,              //氢二钠，盐倍半水
                Sodium = 1.5,                    //乙酸钠
                VortexTime = 60,               //涡旋时间
                VortexVel = 2000,              //涡旋速度
                VibrationTime = 60,            //振荡时间
                VibrationVel = 420,            //振荡速度
                CentrifugalTime = 5,           //离心时间
                CentrifugalVel = 4200,         //离心速度
                ExtractVolume = 8,             //上清液提取量
                ConcentrationTime = 5,         //浓缩时间
                ConcentrationVel = 10000,      //浓缩速度
                Tech = 0xF03EE19,              //工艺
            });

            TechList.Add(new TechParamsModel()
            {
                Name = "GB23200.121-2021 果蔬",

                AddWater = 0,                  //纯水
                ACE = 10,                      //乙腈
                Acid = 0,                      //醋酸
                Formic = 0,                    //甲酸
                Homo = 2,                      //均质子
                MgSO4 = 4,                     //硫酸镁
                NaCl = 1,                      //氯化钠
                Trisodium = 1,                  //柠檬酸钠，二水合物
                Monosodium = 0.5,              //氢二钠，盐倍半水
                Sodium = 0,                    //乙酸钠
                VortexTime = 60,               //涡旋时间
                VortexVel = 2000,              //涡旋速度
                VibrationTime = 60,            //振荡时间
                VibrationVel = 420,            //振荡速度
                CentrifugalTime = 5,           //离心时间
                CentrifugalVel = 4200,         //离心速度
                ExtractVolume = 6,             //上清液提取量
                ConcentrationTime = 5,         //浓缩时间
                ConcentrationVel = 10000,      //浓缩速度
                Tech = 0x801ECF9,              //工艺
            });

            TechList.Add(new TechParamsModel()
            {
                Name = "GB23200.121-2021 坚果、油料",

                AddWater = 10,                  //纯水
                ACE = 15,                      //乙腈
                Acid = 0,                      //醋酸
                Formic = 0,                    //甲酸
                Homo = 2,                      //均质子
                MgSO4 = 0,                     //硫酸镁
                NaCl = 6,                      //氯化钠 硫酸钠
                Trisodium = 0,                  //柠檬酸钠，二水合物
                Monosodium = 0,              //氢二钠，盐倍半水
                Sodium = 1.5,                    //乙酸钠
                VortexTime = 60,               //涡旋时间
                VortexVel = 2000,              //涡旋速度
                VibrationTime = 60,            //振荡时间
                VibrationVel = 420,            //振荡速度
                CentrifugalTime = 5,           //离心时间
                CentrifugalVel = 4200,         //离心速度
                ExtractVolume = 6,             //上清液提取量
                ConcentrationTime = 5,         //浓缩时间
                ConcentrationVel = 10000,      //浓缩速度
                Tech = 0x801ECF9,              //工艺
            });

            TechList.Add(new TechParamsModel()
            {
                Name = "Agilent 多兽药残留",

                AddWater = 1,                  //纯水
                ACE = 0,                      //乙腈
                Acid = 0,                      //醋酸
                Formic = 10,                    //甲酸
                Homo = 2,                      //均质子
                MgSO4 = 0,                     //硫酸镁，硫酸钠
                NaCl = 0,                      //氯化钠
                Trisodium = 0,                  //柠檬酸钠，二水合物
                Monosodium = 0,              //氢二钠，盐倍半水
                Sodium = 0,                    //乙酸钠
                VortexTime = 120,               //涡旋时间
                VortexVel = 2000,              //涡旋速度
                VibrationTime = 120,            //振荡时间
                VibrationVel = 420,            //振荡速度
                CentrifugalTime = 5,           //离心时间
                CentrifugalVel = 4200,         //离心速度
                ExtractVolume = 5,             //上清液提取量
                ConcentrationTime = 10,         //浓缩时间
                ConcentrationVel = 10000,      //浓缩速度
                Tech = 0x3EFDE1AB,              //工艺
            });


            SampleModel = new SampleModel()
            {
                Name1 = "大白菜",
                Name2 = "大葱",
                SnNum1 = "px566",
                SnNum2 = "kx203"
            };


        }





    }

}
