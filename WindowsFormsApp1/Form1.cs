using BQJX.Core;
using BQJX.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Logger _logger = new Logger();
        CardBase _card;
        EtherCATMotion _motion;
        IoDevice _io;
        Task _task;

        bool flag;

        private ushort _axisNo = 0;
        private double _jogVel = 1;
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;

            _card = new CardBase(_logger);

        }

        private void bntJogF_MouseDown(object sender, MouseEventArgs e)
        {
            _motion.JogMove(_axisNo, _jogVel, 1);
        }

        private void bntJogF_MouseUp(object sender, MouseEventArgs e)
        {
            _motion.JogStop(_axisNo);
        }

        private void btnJogR_MouseDown(object sender, MouseEventArgs e)
        {
            _motion.JogMove(_axisNo, _jogVel, 0);
        }

        private void btnJogR_MouseUp(object sender, MouseEventArgs e)
        {
            _motion.JogStop(_axisNo);
        }

        private void btnDisable_Click(object sender, EventArgs e)
        {
            _motion.ServoOff(_axisNo);
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            _motion.ServoOn(_axisNo);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _motion.StopMove(_axisNo);
        }

        private void btnEmgStop_Click(object sender, EventArgs e)
        {
            _motion.Emg_stop(_axisNo);
        }

        private void btnVelMove_Click(object sender, EventArgs e)
        {
            double vel = 1;
            if (!double.TryParse(tbTargetVel.Text,out vel))
            {
                vel = 1;
            }
            _motion.VelocityMove(_axisNo, vel, 1);
        }

        private async void btnAbs_Click(object sender, EventArgs e)
        {
            double target = 0;
            double vel = 0;
            if (!double.TryParse(tbTargePos.Text,out target))
            {
                target = 0;
            }
            if (!double.TryParse(tbTargetVel.Text,out vel))
            {
                vel = 1;
            }
            var ret = await _motion.P2pMoveWithCheckDone(_axisNo, target, vel, null);
            if (ret )
            {
                MessageBox.Show("绝对运动到位！");
            }


        }

        private async void btnRelative_Click(object sender, EventArgs e)
        {
            double target = 0;
            double vel = 0;
            if (!double.TryParse(tbTargePos.Text, out target))
            {
                target = 0;
            }
            if (!double.TryParse(tbTargetVel.Text, out vel))
            {
                vel = 1;
            }
            var ret = await _motion.RelativeMoveWithCheckDone(_axisNo, target, vel, null);
            if (ret)
            {
                MessageBox.Show("相对运动到位！");
            }
        }

        private void btnHomeZore_Click(object sender, EventArgs e)
        {
            _motion.SetPositonToSystem(_axisNo, 0);
        }

        
        private async void btnInitial_Click(object sender, EventArgs e)
        {
            flag = true;
            var ret = await _card.Initialize("").ConfigureAwait(false);
            if (ret != 0)
            {
                MessageBox.Show("板卡初始化出错");
                return;
            }
            List<AxisEleGear> list = new List<AxisEleGear>()
            {
                new AxisEleGear{ AxisName="轴0",AxisNo=0,EleGear = 8388608},
                new AxisEleGear{ AxisName="轴1",AxisNo=1,EleGear = 8388608},
                new AxisEleGear{ AxisName="轴2",AxisNo=2,EleGear = 8388608},
                new AxisEleGear{ AxisName="轴3",AxisNo=3,EleGear = 8388608},
                new AxisEleGear{ AxisName="离心机",AxisNo=4,EleGear = 8388608,Tacc = 10, Tdec = 15},
                new AxisEleGear{ AxisName="振荡",AxisNo=5,EleGear = 8388608}
            };

            _motion = new EtherCATMotion(list, _logger);

            _task =Task.Run(() => 
            {
                while (flag)
                {
                    RefreshStatus();

                    Thread.Sleep(1000);
                }
                
            });

        }

        private async void btnClose_Click(object sender, EventArgs e)
        {
            var ret = await _card.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _axisNo = (ushort)comboBox1.SelectedIndex;
        }



        public static void WriteLog(string mes)
        {
            (Program.form as Form1)?.Write(mes);
        }

        public void Write(string mes)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate 
                {
                    rtb_log.Text += mes + Environment.NewLine;

                }));
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            _motion.ResetAxisAlm(_axisNo);
        }


        private void btnConfigLimit_Click(object sender, EventArgs e)
        {
            _motion.ConfigSoftLimit(_axisNo, 1, 0, 100);
        }


        private async void btn_Reset_Click(object sender, EventArgs e)
        {
            flag = false;
            await Task.Delay(2000).ConfigureAwait(false);

            await _card.ResetFieldBus(0);

            flag = true;
            _task = Task.Run(() =>
            {
                while (flag)
                {
                    RefreshStatus();

                    Thread.Sleep(1000);
                }

            });


            this.Invoke(new MethodInvoker(delegate
            {
                MessageBox.Show("复位完成");
            }));

        }

        public void RefreshStatus()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    var motionIo = _motion.GetMotionIoStatus(_axisNo);
                    cb_alm.Checked = (motionIo & 1) == 1;
                    cb_el_p.Checked = (motionIo & 2) == 2;
                    cb_el_n.Checked = (motionIo & 4) == 4;
                    cb_emg.Checked = (motionIo & 8) == 8;
                    cb_org.Checked = (motionIo & 16) == 16;

                    cb_sl_p.Checked = (motionIo & 64) == 64;
                    cb_sl_n.Checked = (motionIo & 128) == 128;
                    cb_inp.Checked = (motionIo & 256) == 256;
                    cb_rdy.Checked = (motionIo & 512) == 512;
                    cb_dstp.Checked = (motionIo & 1024) == 1024;
                    cb_sevon.Checked = (motionIo & 2048) == 2048;

                    var mStatus = _motion.GetMotionStatus(_axisNo);
                    checkBox1.Checked = (mStatus & 1) == 1;  
                    checkBox2.Checked = (mStatus & 2) == 2;  
                    checkBox3.Checked = (mStatus & 4) == 4;  
                    checkBox4.Checked = (mStatus & 8) == 8;  
                    checkBox5.Checked = (mStatus & 16) == 16;  
                    checkBox6.Checked = (mStatus & 32) == 32;  
                    checkBox7.Checked = (mStatus & 64) == 64;  
                    checkBox8.Checked = (mStatus & 128) == 128;  
                  





                    tbCurrentPos.Text = _motion.GetCurrentPos(_axisNo).ToString();
                    tbCurrentVel.Text = _motion.GetCurrentVel(_axisNo).ToString();


                }));
            }
        }


        private void OutPut_Click(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb == null)
            {
                return;
            }

            if (_io == null)
            {
                _io = new IoDevice(_logger);
            }

            if (!cb.Checked)
            {
                _io.ResetBit_DO(cb.Text);
            }
            else { _io.SetBit_DO(cb.Text); }



        }

        private void btn_set_offset_Click(object sender, EventArgs e)
        {
            _motion.SetOffset(_axisNo);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            flag = false;
            _task?.Wait();
            _card.Close();
        }
    }
}
