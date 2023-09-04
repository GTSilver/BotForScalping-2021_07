using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BotTester
{
    public partial class MainForm : Form
    {
        public static bool cascadeSwitcher = false;
        public static List<Tick> tickList = new List<Tick>();
        public static List<TickOrder> tickOrderList = new List<TickOrder>();
        public static float itemCoefficient;
        public static float itemRevers;

        public MainForm()
        {
            InitializeComponent();
            AnalasedType_DB.SelectedIndex = 0;
            JumpAnalasedType_DB.SelectedIndex = 0;
            JumpRegressionType_DB .SelectedIndex= 0;
            LotType_DB.SelectedIndex = 0;
            TrendRegressionType_DB.SelectedIndex = 0;
            StopLossType_DB.SelectedIndex = 0;
            TakeProfitType_DB.SelectedIndex = 0;
        }

        public class Setup
        {
            public int tradeTickCount;
            public int analasedType;
            public int regressionType;
            public float regressionBase;
            public int lotType;
            public float lotBase;
            public int takeProfitType;
            public float takeProfitBase;
            public int stopLossType;
            public float stopLossBase;
            public int jumpTickCount;
            public int jumpAnalasedType;
            public int jumpRegressionType;
            public float jumpRegressionBase;
            public int jumpValue;
            public float startLot;
            public int startTakeprofit;
            public int startStoploss;
            public float balance;
            public bool revers;
        }

        public void TextBoxBruteForce(TextBox textBox, float start, float finish, float step)
        {
            for (var i = start; i <= finish; i += step)
            {
                textBox.Text = i.ToString();
                ForexSimulation();
            }
        }

        public void ComboBoxBruteForce(ComboBox comboBox, int start, int finish, int step)
        {
            for (var i = start; i <= finish; i += step)
            {
                comboBox.SelectedIndex = i;
                ForexSimulation();
            }
        }

        public enum OrderType
        {
            Up,
            Down,
            Open,
            Close
        }

        public class Tick
        {
            public int time;
            public float buy;
            public float sell;
            public float ping;
        }

        public class TickOrder
        {
            public float time;
            public OrderType type;
        }


        public class Order
        {
            public float startQuotation;
            public float spred;
            public float lot;
            public float costPerItem;
            public float takeprofitQuotation;
            public float stoplossQuotation;
            public OrderType type;

            public void OpenUpOrder(Tick startTick, float lotItem, float takeprofitItem, float stoplossItem)
            {
                type = OrderType.Up;
                startQuotation = startTick.buy;
                CalculateOrder(startTick, lotItem);
                takeprofitQuotation = startQuotation + takeprofitItem / itemRevers;
                stoplossQuotation = startQuotation - stoplossItem / itemRevers;
            }

            public void OpenDownOrder(Tick startTick, float lotItem, float takeprofitItem, float stoplossItem)
            {
                type = OrderType.Down;
                startQuotation = startTick.sell;
                CalculateOrder(startTick, lotItem);
                takeprofitQuotation = startQuotation - takeprofitItem / itemRevers;
                stoplossQuotation = startQuotation + stoplossItem / itemRevers;
            }

            public void CalculateOrder(Tick startTick, float lotItem)
            {
                spred = Math.Abs(startTick.buy - startTick.sell) * itemRevers;
                lot = lotItem;
                costPerItem = lot * itemCoefficient;
            }

            public float CalculateAffect(Tick tick)
            {
                if (type == OrderType.Up)
                    return (tick.sell - startQuotation) * costPerItem;

                if (type == OrderType.Down)
                    return (startQuotation - tick.buy) * costPerItem;

                return -1;
            }

            public bool CheckClosing(Tick tick)
            {
                if (type == OrderType.Up)
                    if (tick.sell >= takeprofitQuotation || tick.sell <= stoplossQuotation)
                        return true;

                if (type == OrderType.Down)
                    if (tick.buy <= takeprofitQuotation || tick.buy >= stoplossQuotation)
                        return true;

                return false;
            }
        }

        private void Graphic_PB_SizeChanged(object sender, EventArgs e)
        {
            GraphicUpdate();
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            GraphicUpdate();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            GraphicUpdate();
        }

        public void OpenInputFile_B_Click(object sender, EventArgs e)
        {
            using (var ofd1 = new OpenFileDialog())
            {
                ofd1.Filter = "txt files (*.txt)|*.txt";
                if (ofd1.ShowDialog() == DialogResult.OK)
                    InputFilePath_TB.Text = ofd1.FileName;
            }
        }

        public void LineColorBuy_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                LineColorBuy_B.BackColor = colorDialog1.Color;
        }

        public void PointColorBuy_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                PointColorBuy_B.BackColor = colorDialog1.Color;
        }

        private void LineColorSell_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                LineColorSell_B.BackColor = colorDialog1.Color;
        }

        private void PointColorSell_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                PointColorSell_B.BackColor = colorDialog1.Color;
        }

        private void PointColorOpen_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                PointColorOpen_B.BackColor = colorDialog1.Color;
        }

        private void PointColorClose_B_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
                PointColorClose_B.BackColor = colorDialog1.Color;
        }

        public void TestGraphic_B_Click(object sender, EventArgs e)
        {
            CheckOptions();

            cascadeSwitcher = false;

            GenerateTestGraphic(100, 90, 10);
        }

        public object LoadRealGraphic()
        {
            List<string> resultList = null;

            StreamReader readingStrim = null;
            try { readingStrim = new StreamReader(InputFilePath_TB.Text); resultList = new List<string>(); }
            catch { MessageBox.Show("Файл недоступен для чтения. Запустите программу от с правами администратора и повторите попытку."); return false; }

            while (!readingStrim.EndOfStream)
                resultList.Add(readingStrim.ReadLine());

            resultList.RemoveAt(0);
            resultList.RemoveAt(resultList.Count - 1);
            readingStrim.Close();

            return resultList;
        }

        private object ConvertToTickList(List<string> inputList)
        {
            List<Tick> resultList = null;

            try
            {
                resultList = new List<Tick>();
                foreach (var element in inputList)
                {
                    var temporaryArray = element.Split(':');
                    var temporaryTick = new Tick();
                    temporaryTick.time = (int)Math.Round(temporaryArray[0].ToFloat(), (int)Math.Log10(itemRevers));
                    temporaryTick.buy = (float)Math.Round(temporaryArray[1].ToFloat(), (int)Math.Log10(itemRevers));
                    temporaryTick.sell = (float)Math.Round(temporaryArray[2].ToFloat(), (int)Math.Log10(itemRevers));
                    if (Ping_TB.Text.ToFloat() != 0)
                        temporaryTick.ping = (float)Math.Round(Ping_TB.Text.ToFloat(), (int)Math.Log10(itemRevers));
                    else
                        temporaryTick.ping = (float)Math.Round(temporaryArray[3].ToFloat(), (int)Math.Log10(itemRevers));
                    resultList.Add(temporaryTick);
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при создании списка тиков.");
                return false;
            }

            inputList = null;

            return resultList;
        }

        public void StartSimulation_B_Click(object sender, EventArgs e)
        {
            ClearData();

            if (!CheckOptions()) return;

            List<string> inputList = null;
            try { inputList = (List<string>)LoadRealGraphic(); } catch { return; };

            try { tickList = (List<Tick>)ConvertToTickList(inputList); } catch { return; };

            inputList.Clear();

            cascadeSwitcher = false;

            var forexSimulation = new Thread(new ThreadStart(ForexSimulation));
            forexSimulation.Start();
        }

        public bool CheckOptions()
        {
            if (AnalasedType_DB.SelectedIndex < 0) { MessageBox.Show("Тип тренда не выбран"); return false; }
            if (JumpAnalasedType_DB.SelectedIndex < 0) { MessageBox.Show("Тип скачка не выбран"); return false; }
            if (JumpRegressionType_DB.SelectedIndex < 0) { MessageBox.Show("Регрессия скачка не выбрана"); return false; }
            if (LotType_DB.SelectedIndex < 0) { MessageBox.Show("Тип лота не выбран"); return false; }
            if (TrendRegressionType_DB.SelectedIndex < 0) { MessageBox.Show("Тип регрессии тренда не выбран"); return false; }
            if (StopLossType_DB.SelectedIndex < 0) { MessageBox.Show("Тип стоплоса не выбран"); return false; }
            if (TakeProfitType_DB.SelectedIndex < 0) { MessageBox.Show("Тип тейкпрофита не выбран"); return false; }

            try { LineSize_TB.Text.ToFloat(); } catch { MessageBox.Show("Величина линий заполнена не корректно"); return false; }
            try { PointSize_TB.Text.ToFloat(); } catch { MessageBox.Show("Величина точек заполнена не корректно"); return false; }

            try { JumpTickCount_TB.Text.ToInt(); } catch { MessageBox.Show("Количество тиков влияющих на скачёк заполнено не корректно"); return false; }
            try { JumpValue_TB.Text.ToInt(); } catch { MessageBox.Show("Величина скачка заполнена не корректно"); return false; }
            try { TradeTickCount_TB.Text.ToInt(); } catch { MessageBox.Show("Количество тиков влияющих на тренд заполнено не корректно"); return false; }

            try { TrendRegressionBase_TB.Text.ToFloat(); } catch { MessageBox.Show("Основание регрессии тренда заполнено не корректно"); return false; }
            try { JumpRegressionBase_TB.Text.ToFloat(); } catch { MessageBox.Show("Основание регрессии скачка заполнено не корректно"); return false; }
            try { TakeProfitBase_TB.Text.ToFloat(); } catch { MessageBox.Show("Основание тейкпрофита заполнено не корректно"); return false; }
            try { StopLossBase_TB.Text.ToFloat(); } catch { MessageBox.Show("Основание стоплосса заполнено не корректно"); return false; }
            try { LotBase_TB.Text.ToFloat(); } catch { MessageBox.Show("Основание лота заполнено не корректно"); return false; }

            try { StartBalance_TB.Text.ToFloat(); } catch { MessageBox.Show("Начальный баланс заполнен не корректно"); return false; }
            try { Accuracy_TB.Text.ToInt(); } catch { MessageBox.Show("Точность символа заполнена не корректно"); return false; }
            try { DealSize_TB.Text.ToInt(); } catch { MessageBox.Show("Величина сделки заполнена не корректно"); return false; }
            try { StartLot_TB.Text.ToFloat(); } catch { MessageBox.Show("Начальный лот заполнен не корректно"); return false; }
            try { StartStoploss_TB.Text.ToInt(); } catch { MessageBox.Show("Начальный стоплосс заполнен не корректно"); return false; }
            try { StartTakeprofit_TB.Text.ToInt(); } catch { MessageBox.Show("Начальный тейкпрофит заполнен не корректно"); return false; }
            try { Ping_TB.Text.ToFloat(); } catch { MessageBox.Show("Пинг заполнен не корректно"); return false; }

            itemRevers = (float)Math.Pow(10, Accuracy_TB.Text.ToFloat());
            itemCoefficient = DealSize_TB.Text.ToFloat() / itemRevers;

            return true;
        }

        public void ClearData()
        {
            tickList.Clear();
            tickOrderList.Clear();
            GC.Collect();
        }

        public void GenerateTestGraphic(int pointCount, float max, float min)
        {
            var randomValue = new Random();

            ClearData();

            for (var i = 0; i < pointCount; i++)
            {
                tickList.Add(new Tick()
                {
                    buy = (float)Math.Round((randomValue.NextDouble() * (max - min) + min), (int)Math.Log10(itemRevers)),
                    sell = (float)Math.Round((randomValue.NextDouble() * (max - min) + min), (int)Math.Log10(itemRevers)),
                    time = i
                });

                if (randomValue.NextDouble() > 0.75 && !cascadeSwitcher)
                    tickOrderList.Add(new TickOrder()
                    {
                        time = tickList[tickList.Count - 1].time,
                        type = OrderType.Open
                    });

                if (randomValue.NextDouble() < 0.25 && !cascadeSwitcher)
                    tickOrderList.Add(new TickOrder()
                    {
                        time = tickList[tickList.Count - 1].time,
                        type = OrderType.Close
                    });
            }

            GraphicUpdate();
        }

        public string[] Simulation(Setup setup)
        {
            var waitingList = new List<Tick>();
            var visibleList = new List<Tick>();

            var startTime = tickList[0].time;
            var tick = 0;
            var currentTime = 0;
            var ping = 0f;
            var timeDelay = 0;
            var successCount = 0;
            var successValue = 0f;
            var failCount = 0;
            var failValue = 0f;
            var analyseSwitcher = false;
            var checkingSwitcher = false;
            var openUpOrder = false;
            var openDownOrder = false;
            var openOrderSignal = false;
            var openedOrderSignal = false;
            var preOpenedOrderSignal = false;
            var order = new Order();
            var lot = setup.startLot;
            var takeprofit = setup.startTakeprofit;
            var stoploss = setup.startStoploss;

            var balance = setup.balance;

            while (tick < tickList.Count)
            {
                //Симуляция изменения рынка
                if (startTime + currentTime >= tickList[tick].time)
                {
                    waitingList.Add(tickList[tick]);
                    tick++;
                }

                //Симуляция видимых изменений на рынке
                for (var i = 0; i < waitingList.Count; i++)
                {
                    if ((int)((float)startTime + (float)currentTime - (float)waitingList[i].ping) >= waitingList[i].time)
                    {
                        visibleList.Add(waitingList[i]);
                        waitingList.RemoveAt(i);
                        i--;
                        analyseSwitcher = true;
                        checkingSwitcher = true;
                    }
                    else
                        break;
                }

                //Увидено достаточное количество тиков для анализа тренда и скачков
                if (visibleList.Count > setup.tradeTickCount && visibleList.Count > setup.jumpTickCount && analyseSwitcher)
                {
                    analyseSwitcher = false;
                    var trendTick = TrendAnalyze(setup, TradeRegression(setup, visibleList));
                    var jumpTick = JumpAnalyze(setup, JumpRegression(setup, visibleList));

                    if (trendTick.buy - jumpTick.buy >= setup.jumpValue && !preOpenedOrderSignal && !openOrderSignal && setup.revers)
                    {
                        openUpOrder = true;
                        preOpenedOrderSignal = true;
                    }
                    else if (jumpTick.sell - trendTick.sell >= setup.jumpValue && !preOpenedOrderSignal && !openOrderSignal && setup.revers)
                    {
                        openDownOrder = true;
                        preOpenedOrderSignal = true;
                    }
                    else if (jumpTick.buy - trendTick.buy >= setup.jumpValue && !preOpenedOrderSignal && !openOrderSignal && !setup.revers)
                    {
                        openUpOrder = true;
                        preOpenedOrderSignal = true;
                    }
                    else if (trendTick.sell - jumpTick.sell >= setup.jumpValue && !preOpenedOrderSignal && !openOrderSignal && !setup.revers)
                    {
                        openDownOrder = true;
                        preOpenedOrderSignal = true;
                    }
                }

                //Начало отсчёта задержки в открытии ордера
                if (preOpenedOrderSignal && ping == 0 && !openedOrderSignal)
                {
                    ping = tickList[tick].ping;
                    timeDelay = 0;
                }

                //Ожидание поступления сигнала с задержкой на открытие ордера
                if (timeDelay > ping && preOpenedOrderSignal && !openedOrderSignal)
                {
                    openOrderSignal = true;
                    ping = 0;
                    preOpenedOrderSignal = false;
                }

                //Открытие нового ордера
                if (openOrderSignal && !openedOrderSignal)
                {
                    if (openUpOrder)
                    {
                        order.OpenUpOrder(tickList[tick], lot, takeprofit, stoploss);
                        if (!cascadeSwitcher)
                            tickOrderList.Add(new TickOrder
                            {
                                time = tickList[tick].time,
                                type = OrderType.Open
                            });
                    }

                    if (openDownOrder)
                    {
                        order.OpenDownOrder(tickList[tick], lot, takeprofit, stoploss);
                        if (!cascadeSwitcher)
                            tickOrderList.Add(new TickOrder
                            {
                                time = tickList[tick].time,
                                type = OrderType.Open
                            });
                    }

                    openOrderSignal = false;
                    openedOrderSignal = true;
                }

                //Проверка ордера на закрытие и закрытие
                if (openedOrderSignal && checkingSwitcher)
                {
                    if (order.CheckClosing(visibleList[visibleList.Count - 1]))
                    {
                        if (openUpOrder && !cascadeSwitcher)
                            tickOrderList.Add(new TickOrder
                            {
                                time = tickList[tick].time,
                                type = OrderType.Close
                            });

                        if (openDownOrder && !cascadeSwitcher)
                            tickOrderList.Add(new TickOrder
                            {
                                time = tickList[tick].time,
                                type = OrderType.Close
                            });

                        if (order.CalculateAffect(tickList[tick]) > 0)
                        {
                            successCount++;
                            successValue += (float)Math.Round(order.CalculateAffect(tickList[tick]), (int)Math.Log10(itemRevers));
                            lot = SetLot(lot, true, setup);
                            takeprofit = SetTakeprofit(takeprofit, true, setup);
                            stoploss = SetStoploss(stoploss, true, setup);
                        }

                        if (order.CalculateAffect(tickList[tick]) < 0)
                        {
                            failCount++;
                            failValue += (float)Math.Round(order.CalculateAffect(tickList[tick]), (int)Math.Log10(itemRevers));
                            lot = SetLot(lot, true, setup);
                            takeprofit = SetTakeprofit(takeprofit, false, setup);
                            stoploss = SetStoploss(stoploss, false, setup);
                        }

                        openedOrderSignal = false;

                        balance += (float)Math.Round(order.CalculateAffect(tickList[tick]), (int)Math.Log10(itemRevers));
                    }
                    checkingSwitcher = false;
                }

                currentTime++;
                timeDelay++;
            }
            var result = new string[]
            {   "Успехов:|" + successCount.ToString() + "|" + successValue.ToString(),
                "Неудач:|" + failCount.ToString() + "|" + failValue.ToString(),
                "Баланс:|" + balance.ToString(),
                "Коэффициент:|" + (successValue / -failValue).ToString()
            };

            waitingList.Clear();
            visibleList.Clear();

            return result;
        }

        public float SetLot(float lot, bool success, Setup setup)
        {
            if (setup.lotType == 0) return lot;
            if (setup.lotType == 1 && success) return (float)Math.Round((lot + setup.lotBase), (int)Math.Log10(itemRevers));
            if (setup.lotType == 1 && !success) return (float)Math.Round((lot - setup.lotBase), (int)Math.Log10(itemRevers));
            if (setup.lotType == 2 && success) return (float)Math.Round((lot * setup.lotBase), (int)Math.Log10(itemRevers));
            if (setup.lotType == 2 && !success) return (float)Math.Round((lot / setup.lotBase), (int)Math.Log10(itemRevers));
            return -1;
        }

        public int SetTakeprofit(int takeprofit, bool success, Setup setup)
        {
            if (setup.takeProfitType == 0) return takeprofit;
            if (setup.takeProfitType == 1 && success) return (short)(Math.Round((takeprofit + setup.takeProfitBase), (int)Math.Log10(itemRevers)));
            if (setup.takeProfitType == 1 && !success) return (short)(Math.Round((takeprofit - setup.takeProfitBase), (int)Math.Log10(itemRevers)));
            if (setup.takeProfitType == 2 && success) return (short)(Math.Round((takeprofit * setup.takeProfitBase), (int)Math.Log10(itemRevers)));
            if (setup.takeProfitType == 2 && !success) return (short)(Math.Round((takeprofit / setup.takeProfitBase), (int)Math.Log10(itemRevers)));
            return -1;
        }

        public int SetStoploss(int stoploss, bool success, Setup setup)
        {
            if (setup.stopLossType == 0) return stoploss;
            if (setup.stopLossType == 1 && success) return (short)(Math.Round((stoploss - setup.stopLossBase) * (int)Math.Log10(itemRevers)));
            if (setup.stopLossType == 1 && !success) return (short)(Math.Round((stoploss + setup.stopLossBase) * (int)Math.Log10(itemRevers)));
            if (setup.stopLossType == 2 && success) return (short)(Math.Round((stoploss / setup.stopLossBase) * (int)Math.Log10(itemRevers)));
            if (setup.stopLossType == 2 && !success) return (short)(Math.Round((stoploss * setup.stopLossBase) * (int)Math.Log10(itemRevers)));
            return -1;
        }

        public void GetSetup(Setup setup)
        {
            Invoke((MethodInvoker)delegate ()
            {
                setup.tradeTickCount = TradeTickCount_TB.Text.ToInt();
                setup.jumpTickCount = JumpTickCount_TB.Text.ToInt();
                setup.jumpValue = JumpValue_TB.Text.ToInt();
                setup.startLot = StartLot_TB.Text.ToFloat();
                setup.startTakeprofit = StartTakeprofit_TB.Text.ToInt();
                setup.startStoploss = StartStoploss_TB.Text.ToInt();
                setup.regressionType = TrendRegressionType_DB.SelectedIndex;
                setup.regressionBase = TrendRegressionBase_TB.Text.ToFloat();
                setup.jumpRegressionType = JumpRegressionType_DB.SelectedIndex;
                setup.jumpRegressionBase = JumpRegressionBase_TB.Text.ToFloat();
                setup.tradeTickCount = TradeTickCount_TB.Text.ToInt();
                setup.analasedType = AnalasedType_DB.SelectedIndex;
                setup.jumpTickCount = JumpTickCount_TB.Text.ToInt();
                setup.jumpAnalasedType = JumpAnalasedType_DB.SelectedIndex;
                setup.balance = StartBalance_TB.Text.ToFloat();
                setup.revers = Revers_CB.Checked;
            });
        }

        public void ForexSimulation()
        {
            var setup = new Setup();
            GetSetup(setup);

            var result = Simulation(setup);

            Invoke((MethodInvoker)delegate () { GraphicUpdate(); });

            MessageBox.Show("Симуляция завершена.\n" + result[0] + "\n" + result[1] + "\n" + result[2] + "\n" + result[3]);
        }

        public List<Tick> ProportionalityRegression(List<Tick> tickL)
        {
            var result = new List<Tick>();
            int number = 1;

            for (var i = 0; i < tickL.Count; i++)
                result.Add(new Tick());

            for (var i = tickL.Count - 1; i >= 0; i--)
            {
                result[i].time = tickL[i].time;
                result[i].buy = (float)Math.Round(tickL[i].buy / number * itemRevers);
                result[i].sell = (float)Math.Round(tickL[i].sell / number * itemRevers);
                result[i].ping = tickL[i].ping;
                number++;
            }

            return result;
        }

        public List<Tick> PolynomialRegression(List<Tick> tickL, float baseValue)
        {
            var result = new List<Tick>();
            int number = 1;

            for (var i = 0; i < tickL.Count; i++)
                result.Add(new Tick());

            for (var i = tickL.Count - 1; i >= 0; i--)
            {
                result[i].time = tickL[i].time;
                result[i].buy = (float)Math.Round(tickL[i].buy / Math.Pow(number, baseValue) * itemRevers);
                result[i].sell = (float)Math.Round(tickL[i].sell / Math.Pow(number, baseValue) * itemRevers);
                result[i].ping = tickL[i].ping;
                number++;
            }

            return result;
        }

        public List<Tick> FibonacciRegression(List<Tick> tickL, float baseValue)
        {
            var result = new List<Tick>();
            int number = 1;

            var fibonacciList = new List<float>() { baseValue, baseValue + baseValue };

            for (var i = 0; i < tickL.Count; i++)
            {
                result.Add(new Tick());
                fibonacciList.Add(fibonacciList[i] + fibonacciList[i + 1]);
            }

            for (var i = tickL.Count - 1; i >= 0; i--)
            {
                result[i].time = tickL[i].time;
                result[i].buy = (float)Math.Round(tickL[i].buy / fibonacciList[number - 1] * itemRevers);
                result[i].sell = (float)Math.Round(tickL[i].sell / fibonacciList[number - 1] * itemRevers);
                result[i].ping = tickL[i].ping;
                number++;
            }

            return result;
        }

        public List<Tick> ExponentialRegression(List<Tick> tickL, float baseValue)
        {
            var result = new List<Tick>();
            int number = 1;

            for (var i = 0; i < tickL.Count; i++)
                result.Add(new Tick());

            for (var i = tickL.Count - 1; i >= 0; i--)
            {
                result[i].time = tickL[i].time;
                result[i].buy = (float)Math.Round(tickL[i].buy / Math.Pow(baseValue, number) * itemRevers);
                result[i].sell = (float)Math.Round(tickL[i].sell / Math.Pow(baseValue, number) * itemRevers);
                result[i].ping = tickL[i].ping;
                number++;
            }

            return result;
        }

        public List<Tick> FactorialRegression(List<Tick> tickL)
        {
            var result = new List<Tick>();
            int number = 1;

            var factorialList = new List<float>() { 1, 2, 6, 24, 120, 720, 5040 };

            for (var i = 0; i < tickL.Count; i++)
            {
                result.Add(new Tick());
                factorialList.Add(factorialList[i] + factorialList[i + 1]);
            }

            for (var i = tickL.Count - 1; i >= 0; i--)
            {
                result[i].time = tickL[i].time;
                result[i].ping = tickL[i].ping;

                if (number < 8)
                {
                    result[i].buy = (float)Math.Round(tickL[i].buy / factorialList[number - 1] * itemRevers);
                    result[i].sell = (float)Math.Round(tickL[i].sell / factorialList[number - 1] * itemRevers);
                }
                else
                {
                    result[i].buy = 0;
                    result[i].sell = 0;
                }

                number++;
            }

            return result;
        }

        public List<Tick> TradeRegression(Setup setup, List<Tick> tickL)
        {
            switch (setup.regressionType)
            {
                case 1: return ProportionalityRegression(tickL);
                case 2: return PolynomialRegression(tickL, setup.regressionBase);
                case 3: return FibonacciRegression(tickL, setup.regressionBase);
                case 4: return ExponentialRegression(tickL, setup.regressionBase);
                case 5: return FactorialRegression(tickL);
            }

            return tickL;
        }

        public List<Tick> JumpRegression(Setup setup, List<Tick> tickL)
        {
            switch (setup.jumpRegressionType)
            {
                case 1: return ProportionalityRegression(tickL);
                case 2: return PolynomialRegression(tickL, setup.jumpRegressionBase);
                case 3: return FibonacciRegression(tickL, setup.jumpRegressionBase);
                case 4: return ExponentialRegression(tickL, setup.jumpRegressionBase);
                case 5: return FactorialRegression(tickL);
            }

            return tickL;
        }

        public Tick ArithmeticMean(int tickCount, List<Tick> tickL)
        {
            float buy = 0;
            float sell = 0;

            for (var i = tickL.Count - 1; i > tickL.Count - tickCount - 1; i--)
            {
                buy += tickL[i].buy;
                sell += tickL[i].sell;
            }

            return new Tick()
            {
                time = -1,
                buy = (float)Math.Round(buy / tickCount * itemRevers),
                sell = (float)Math.Round(sell / tickCount * itemRevers),
                ping = tickL[tickL.Count - 1].ping
            };
        }

        public Tick GeometricMean(int tickCount, List<Tick> tickL)
        {
            float buy = 0;
            float sell = 0;

            for (var i = tickL.Count - 1; i > tickL.Count - tickCount - 1; i--)
            {
                buy *= tickL[i].buy;
                sell *= tickL[i].sell;
            }

            return new Tick()
            {
                time = -1,
                buy = (float)Math.Round(Math.Pow(buy, 1 / tickCount) * itemRevers),
                sell = (float)Math.Round(Math.Pow(sell, 1 / tickCount) * itemRevers),
                ping = tickL[tickL.Count - 1].ping
            };
        }

        public Tick HarmonicMean(int tickCount, List<Tick> tickL)
        {
            float buy = 0;
            float sell = 0;

            for (var i = tickL.Count - 1; i > tickL.Count - tickCount - 1; i--)
            {
                buy += 1 / tickL[i].buy;
                sell += 1 / tickL[i].sell;
            }

            return new Tick()
            {
                time = -1,
                buy = (float)Math.Round(tickCount / buy * itemRevers),
                sell = (float)Math.Round(tickCount / sell * itemRevers),
                ping = tickL[tickL.Count - 1].ping
            };
        }

        public Tick RootSquareMean(int tickCount, List<Tick> tickL)
        {
            float buy = 0;
            float sell = 0;

            for (var i = tickL.Count - 1; i > tickL.Count - tickCount - 1; i--)
            {
                buy += tickL[i].buy * tickL[i].buy;
                sell += tickL[i].sell * tickL[i].sell;
            }

            return new Tick()
            {
                time = -1,
                buy = (float)Math.Round(Math.Sqrt(buy / tickCount) * itemRevers),
                sell = (float)Math.Round(Math.Sqrt(sell / tickCount) * itemRevers),
                ping = tickL[tickL.Count - 1].ping
            };
        }

        public Tick CubicMean(int tickCount, List<Tick> tickL)
        {
            float buy = 0;
            float sell = 0;

            for (var i = tickL.Count - 1; i > tickL.Count - tickCount - 1; i--)
            {
                buy += tickL[i].buy * tickL[i].buy * tickL[i].buy;
                sell += tickL[i].sell * tickL[i].sell * tickL[i].sell;
            }

            return new Tick()
            {
                time = -1,
                buy = (float)Math.Round(Math.Pow(buy / tickCount, 1d / 3d) * itemRevers),
                sell = (float)Math.Round(Math.Pow(sell / tickCount, 1d / 3d) * itemRevers),
                ping = tickL[tickL.Count - 1].ping
            };
        }

        public Tick TrendAnalyze(Setup setup, List<Tick> tickL)
        {
            switch (setup.analasedType)
            {
                case 0: return ArithmeticMean(setup.tradeTickCount, tickL);
                case 1: return GeometricMean(setup.tradeTickCount, tickL);
                case 2: return HarmonicMean(setup.tradeTickCount, tickL);
                case 3: return RootSquareMean(setup.tradeTickCount, tickL);
                case 4: return CubicMean(setup.tradeTickCount, tickL);
            }
            
            return null;
        }

        public Tick JumpAnalyze(Setup setup, List<Tick> tickL)
        {
            switch (setup.jumpAnalasedType)
            {
                case 0: return ArithmeticMean(setup.jumpTickCount, tickL);
                case 1: return GeometricMean(setup.jumpTickCount, tickL);
                case 2: return HarmonicMean(setup.jumpTickCount, tickL);
                case 3: return RootSquareMean(setup.jumpTickCount, tickL);
                case 4: return CubicMean(setup.jumpTickCount, tickL);
            }

            return null;
        }

        public void GraphicUpdate()
        {
            if (!CheckOptions()) return;

            if (tickList.Count <= 0) return;

            var showTickList = new List<Tick>();
            var showTickOrderList = new List<TickOrder>();

            //Создание инструментов для рисования
            var lineBuyPen = new Pen(new SolidBrush(LineColorBuy_B.BackColor), LineSize_TB.Text.ToFloat());
            var pointBuyPen = new Pen(new SolidBrush(PointColorBuy_B.BackColor), PointSize_TB.Text.ToFloat());
            var lineSellPen = new Pen(new SolidBrush(LineColorSell_B.BackColor), LineSize_TB.Text.ToFloat());
            var pointSellPen = new Pen(new SolidBrush(PointColorSell_B.BackColor), PointSize_TB.Text.ToFloat());
            var pointOpenPen = new Pen(new SolidBrush(PointColorOpen_B.BackColor), PointSize_TB.Text.ToFloat());
            var pointClosePen = new Pen(new SolidBrush(PointColorClose_B.BackColor), PointSize_TB.Text.ToFloat());
            var textPen = new Pen(new SolidBrush(Color.White), 1);
            var gridPen = new Pen(new SolidBrush(Color.FromArgb(255, 64, 64, 64)), 1);

            //Скалирование и ограничение списка данных
            var scrollBarSize = (int)Math.Ceiling(1000d * (float)trackBar1.Value / (float)trackBar1.Maximum);
            hScrollBar1.LargeChange = scrollBarSize;
            Scale_TB.Text = (scrollBarSize / 10d).ToString() + "%";
            if (hScrollBar1.Value + hScrollBar1.LargeChange > hScrollBar1.Maximum)
                hScrollBar1.Value = hScrollBar1.Maximum - hScrollBar1.LargeChange;

            var startTime = tickList[0].time;
            var endTime = tickList[tickList.Count - 1].time;

            var leftTime = Math.Round((endTime - startTime) * (float)hScrollBar1.Value / (float)hScrollBar1.Maximum);
            var rightTime = leftTime + Math.Round((endTime - startTime) * (float)trackBar1.Value / (float)trackBar1.Maximum);

            var maxHeightValue = float.MinValue;
            var minHeightValue = float.MaxValue;
            var maxWidthValue = rightTime;
            var minWidthValue = leftTime;
            for (var i = 0; i < tickList.Count - 1; i++)
                if (tickList[i].time - startTime >= leftTime && tickList[i].time - startTime <= rightTime)
                {
                    showTickList.Add(tickList[i]);

                    if (tickList[i].buy > maxHeightValue)
                        maxHeightValue = tickList[i].buy;
                    
                    if (tickList[i].sell > maxHeightValue)
                        maxHeightValue = tickList[i].sell;

                    if (tickList[i].buy < minHeightValue)
                        minHeightValue = tickList[i].buy;
                    
                    if (tickList[i].sell < minHeightValue)
                        minHeightValue = tickList[i].sell;
                }

            for (var i = 0; i < tickOrderList.Count - 1; i++)
                if (tickOrderList[i].time - startTime >= leftTime && tickOrderList[i].time - startTime <= rightTime)
                    showTickOrderList.Add(tickOrderList[i]);

            var bmp = new Bitmap(Graphic_PB.Width, Graphic_PB.Height);
            var graphic = Graphics.FromImage(bmp);

            var boadTopSize = 16;
            var boadBottomSize = 32;
            var boadLeftSize = 32;
            var gridSize = 64;

            var lastGridedX = 0;
            for (var i = 0; i < showTickList.Count - 1; i++)
            {
                //Основные точки котировок
                var pointBuy1 = new Point(
                    (int)Math.Round(boadLeftSize + (showTickList[i].time - minWidthValue - startTime) * (bmp.Width - boadLeftSize) / (maxWidthValue - minWidthValue)),
                    bmp.Height - (int)Math.Round((showTickList[i].buy - minHeightValue) / (maxHeightValue - minHeightValue) * (bmp.Height - boadTopSize - boadBottomSize)) - boadBottomSize);
                
                var pointBuy2 = new Point(
                    (int)Math.Round(boadLeftSize + (showTickList[i + 1].time - minWidthValue - startTime) * (bmp.Width - boadLeftSize) / (maxWidthValue - minWidthValue)),
                    bmp.Height - (int)Math.Round((showTickList[i + 1].buy - minHeightValue) / (maxHeightValue - minHeightValue) * (bmp.Height - boadTopSize - boadBottomSize)) - boadBottomSize);
                
                var pointSell1 = new Point(
                    (int)Math.Round(boadLeftSize + (showTickList[i].time - minWidthValue - startTime) * (bmp.Width - boadLeftSize) / (maxWidthValue - minWidthValue)),
                    bmp.Height - (int)Math.Round((showTickList[i].sell - minHeightValue) / (maxHeightValue - minHeightValue) * (bmp.Height - boadTopSize - boadBottomSize)) - boadBottomSize);
                
                var pointSell2 = new Point(
                    (int)Math.Round(boadLeftSize + (showTickList[i + 1].time - minWidthValue - startTime) * (bmp.Width - boadLeftSize) / (maxWidthValue - minWidthValue)),
                    bmp.Height - (int)Math.Round((showTickList[i + 1].sell - minHeightValue) / (maxHeightValue - minHeightValue) * (bmp.Height - boadTopSize - boadBottomSize)) - boadBottomSize);

                //Рисование сетки и надписей
                if (gridSize < pointBuy1.X - lastGridedX)
                {
                    graphic.DrawLineVerical(gridPen, pointBuy1.X);
                    lastGridedX = pointBuy1.X;
                    graphic.DrawString((showTickList[i].time - startTime).ToString(), DefaultFont, textPen.Brush, new Point(pointBuy1.X, bmp.Height - boadBottomSize));
                }

                //Рисование графика Buy
                graphic.DrawPoint(pointBuyPen, pointBuy1, pointBuyPen.Width);
                graphic.DrawPoint(pointBuyPen, pointBuy2, pointBuyPen.Width);
                graphic.DrawLine(lineBuyPen, pointBuy1, pointBuy2);

                //Рисование графика Sell
                graphic.DrawPoint(pointSellPen, pointSell1, pointSellPen.Width);
                graphic.DrawPoint(pointSellPen, pointSell2, pointSellPen.Width);
                graphic.DrawLine(lineSellPen, pointSell1, pointSell2);

                //Рисование точек открытия и закрытия ордера
                for (var j = 0; j < showTickOrderList.Count; j++)
                {
                    if (showTickList[i].time == showTickOrderList[j].time)
                    {
                        if (showTickOrderList[j].type == OrderType.Open)
                            graphic.DrawPoint(pointOpenPen,  new Point() { X = pointBuy1.X, Y = (int)(bmp.Height - (boadBottomSize - 16)) }, pointOpenPen.Width);

                        if (showTickOrderList[j].type == OrderType.Close)
                            graphic.DrawPoint(pointClosePen, new Point() { X = pointBuy1.X, Y = (int)(bmp.Height - (boadBottomSize - 24)) }, pointClosePen.Width);
                    }

                    if (showTickList[i + 1].time == showTickOrderList[j].time)
                    {
                        if (showTickOrderList[j].type == OrderType.Open)
                            graphic.DrawPoint(pointOpenPen, new Point() { X = pointBuy2.X, Y = (int)(bmp.Height - (boadBottomSize - 16)) }, pointOpenPen.Width);

                        if (showTickOrderList[j].type == OrderType.Close)
                            graphic.DrawPoint(pointClosePen, new Point() { X = pointBuy2.X, Y = (int)(bmp.Height - (boadBottomSize - 24)) }, pointClosePen.Width);
                    }
                }
            }

            Graphic_PB.Image = bmp;
        }

        private void JumpCalculate_B_Click(object sender, EventArgs e)
        {
            if (!CheckOptions()) return;

            List<string> inputList = null;
            try { inputList = (List<string>)LoadRealGraphic(); } catch { return; };

            List<Tick> tickL = null;
            try { tickL = (List<Tick>)ConvertToTickList(inputList); } catch { return; };

            var result = new Tick() { buy = 0, sell = 0 };

            for (var i = 0; i < tickL.Count - 2; i++)
            {
                if (tickL[i + 1].time - tickL[i].time > 0)
                {
                    result.buy += Math.Abs(tickL[i + 1].buy - tickL[i].buy) / Math.Abs(tickL[i + 1].time - tickL[i].time) * itemRevers;
                    result.sell += Math.Abs(tickL[i + 1].sell - tickL[i].sell) / Math.Abs(tickL[i + 1].time - tickL[i].time) * itemRevers;
                }
            }

            var itemValue = Math.Ceiling(Math.Round((result.buy + result.sell) / 2, 0));
            if (itemValue < 1) itemValue = 1;

            JumpValue_TB.Text = itemValue.ToString();
        }
    }

    public static class StringExtention
    {
        public static int ToInt(this string input)
        {
            var result = "";
            if (input.Contains(","))
            {
                var temporary = input.Split(',');
                result = temporary[0] + "." + temporary[1];
            }
            else
                result = input;
            return int.Parse(result, CultureInfo.InvariantCulture);
        }

        public static short ToShort(this string input)
        {
            var result = "";
            if (input.Contains(","))
            {
                var temporary = input.Split(',');
                result = temporary[0] + "." + temporary[1];
            }
            else
                result = input;
            return short.Parse(result, CultureInfo.InvariantCulture);
        }

        public static byte ToByte(this string input)
        {
            var result = "";
            if (input.Contains(","))
            {
                var temporary = input.Split(',');
                result = temporary[0] + "." + temporary[1];
            }
            else
                result = input;
            return byte.Parse(result, CultureInfo.InvariantCulture);
        }

        public static double ToDouble(this string input)
        {
            var result = "";
            if (input.Contains(","))
            {
                var temporary = input.Split(',');
                result = temporary[0] + "." + temporary[1];
            }
            else
                result = input;
            return double.Parse(result, CultureInfo.InvariantCulture);
        }

        public static float ToFloat(this string input)
        {
            var result = "";
            if (input.Contains(","))
            {
                var temporary = input.Split(',');
                result = temporary[0] + "." + temporary[1];
            }
            else
                result = input;
            return float.Parse(result, CultureInfo.InvariantCulture);
        }
    }

    public static class GraphicsExtention
    {
        public static void DrawPoint(this Graphics graphic, Pen pen, float X, float Y, float size)
        {
            graphic.DrawEllipse(pen, new Rectangle() { X = (int)(X - size / 2), Y = (int)(Y - size / 2), Width = (int)size, Height = (int)size });
        }

        public static void DrawPoint(this Graphics graphic, Pen pen, Point point, float size)
        {
            graphic.DrawEllipse(pen, new Rectangle() { X = (int)(point.X - size / 2), Y = (int)(point.Y - size / 2), Width = (int)size, Height = (int)size });
        }

        public static void DrawLineHorizontal(this Graphics graphic, Pen pen, float Y)
        {
            graphic.DrawLine(pen, new Point(0, (int)Y), new Point((int)graphic.ClipBounds.Right, (int)Y));
        }

        public static void DrawLineVerical(this Graphics graphic, Pen pen, float X)
        {
            graphic.DrawLine(pen, new Point((int)X, 0), new Point((int)X, (int)graphic.ClipBounds.Bottom));
        }
    }
}
