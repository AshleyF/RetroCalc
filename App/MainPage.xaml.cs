using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using RetroCalc.Engine;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RetroCalc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Calc calc;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void MainPage_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            switch (args.KeyCode)
            {
                case 94: UserKey('^'); break;
                case 108: UserKey('g'); break; // l
                case 110: UserKey('l'); break; // n
                case 101: UserKey('e'); break;
                case 113: UserKey('q'); break;
                case 97: Arc(null, null); break;
                case 115: UserKey('s'); break;
                case 99: UserKey('c'); break;
                case 116: UserKey('t'); break;
                case 105: UserKey('f'); break; // i
                case 119: UserKey('w'); break;
                case 100: UserKey('r'); break;
                case 62: UserKey('>'); break;
                case 60: UserKey('<'); break;
                case 13: UserKey(' '); break;
                case 32: UserKey(' '); break;
                case 126: UserKey('~'); break;
                case 120: UserKey('x'); break;
                case 8: UserKey(','); break;
                case 45: UserKey('-'); break;
                case 43: UserKey('+'); break;
                case 42: UserKey('*'); break;
                case 47: UserKey('/'); break;
                case 46: UserKey('.'); break;
                case 112: UserKey('p'); break;
                case 48: UserKey('0'); break;
                case 49: UserKey('1'); break;
                case 50: UserKey('2'); break;
                case 51: UserKey('3'); break;
                case 52: UserKey('4'); break;
                case 53: UserKey('5'); break;
                case 54: UserKey('6'); break;
                case 55: UserKey('7'); break;
                case 56: UserKey('8'); break;
                case 57: UserKey('9'); break;
                case 114: Record(null, null); break;
                case 102: PlayF(null, null); break;
                case 103: PlayG(null, null); break;
                case 104: PlayH(null, null); break;
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //EnterBtn.Focus(Windows.UI.Xaml.FocusState.Keyboard);
        }

        private List<char> buffer = new List<char>();

        private char Input()
        {
            if (buffer.Count > 0)
            {
                var k = buffer[0];
                buffer.RemoveAt(0);
                if (buffer.Count > 0) // delay macro playback
                    System.Threading.Tasks.Task.Delay(50).Wait();
                return k;
            }
            else return '\0';
        }

        private void Output(Tuple<string, string, string, string, string> state, int[][] r, int[] s)
        {
            var x = state.Item1;
            if (x == " 0000000000 00") x = " 0.            "; // TODO: stupid bug fix
            var y = state.Item2;
            var z = state.Item3;
            var t = state.Item4;
            var m = state.Item5;
            if (x.Length == 15 || m.Length > 0)
                SaveSettings(r, s);
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var fontSize = 36;
                if (x.Length == 15)
                {
                    if (x[13] != ' ' && (x[11] != ' ' || x[10] != ' ')) // exponent and long num?
                        fontSize = 30;
                    DisplayLeft.FontSize = DisplayRight.FontSize = fontSize;
                    DisplayLeft.Text = x.Substring(0, 12);
                    DisplayRight.Text = x.Substring(12);
                }
                else
                {
                    //DisplayLeft.Text = DisplayRight.Text = "";
                }
                if (m.Length > 0) // not blank?
                {
                    DisplayMemory.Text = m;
                    DisplayStack.Text = String.Format("{2}\n{1}\n{0}", y, z, t);
                }
                //System.Threading.Tasks.Task.Delay(100).Wait();
            }).AsTask().Wait();
        }

        private void SaveSettings(int[][] r, int[] s)
        {
            var appData = Windows.Storage.ApplicationData.Current;
            var roamingSettings = appData.RoamingSettings;
            roamingSettings.Values["F"] = macros[0];
            roamingSettings.Values["G"] = macros[1];
            roamingSettings.Values["H"] = macros[2];
            roamingSettings.Values["S"] = s;
            for (var i = 0; i < 8; i++)
                roamingSettings.Values[string.Format("R{0}", i)] = r[i];
        }

        private void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            // load settings
            int[][] regs = null;
            int[] s = null;
            try
            {
                var appData = Windows.Storage.ApplicationData.Current;
                var roamingSettings = appData.RoamingSettings;

                var f = (string)roamingSettings.Values["F"];
                if (f != null) macros[0] = f;

                var g = (string)roamingSettings.Values["G"];
                if (g != null) macros[1] = g;

                var h = (string)roamingSettings.Values["H"];
                if (h != null) macros[2] = h;

                s = (int[])roamingSettings.Values["S"];
                for (var i = 0; i < 8; i++)
                {
                    var r = (int[])roamingSettings.Values[string.Format("R{0}", i)];
                    if (r != null)
                    {
                        if (regs == null)
                            regs = new int[8][];
                        regs[i] = r;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                // corrupt settings?!
                throw ex;                
#endif
            }

            // init calc
            calc = new Calc(Input, Output, regs, s);
            Windows.System.Threading.ThreadPool.RunAsync(_ => { calc.Emulate(); });
            CoreWindow.GetForCurrentThread().CharacterReceived += MainPage_CharacterReceived;
        }

        private void Key(char k)
        {
            buffer.Add(k);
            if (recording && k != '\0')
                recorded += k;
            selectingFunction = false;
            SinButton.BorderBrush = CosButton.BorderBrush = TanButton.BorderBrush = secondaryButtonBrush;
            if (!recording)
                FButton.BorderBrush = GButton.BorderBrush = HButton.BorderBrush = secondaryButtonBrush;
        }

        private void UserKey(char k)
        {
            Key(k);
        }

        private void Pi(object sender, RoutedEventArgs e)
        {
            UserKey('p');
        }

        private void Dot(object sender, RoutedEventArgs e)
        {
            UserKey('.');
        }

        private void Zero(object sender, RoutedEventArgs e)
        {
            UserKey('0');
        }

        private void Divide(object sender, RoutedEventArgs e)
        {
            UserKey('/');
        }

        private void Three(object sender, RoutedEventArgs e)
        {
            UserKey('3');
        }

        private void Two(object sender, RoutedEventArgs e)
        {
            UserKey('2');
        }

        private void One(object sender, RoutedEventArgs e)
        {
            UserKey('1');
        }

        private void Multiply(object sender, RoutedEventArgs e)
        {
            UserKey('*');
        }

        private void Six(object sender, RoutedEventArgs e)
        {
            UserKey('6');
        }

        private void Five(object sender, RoutedEventArgs e)
        {
            UserKey('5');
        }

        private void Four(object sender, RoutedEventArgs e)
        {
            UserKey('4');
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            UserKey('+');
        }

        private void Nine(object sender, RoutedEventArgs e)
        {
            UserKey('9');
        }

        private void Eight(object sender, RoutedEventArgs e)
        {
            UserKey('8');
        }

        private void Seven(object sender, RoutedEventArgs e)
        {
            UserKey('7');
        }

        private void Subtract(object sender, RoutedEventArgs e)
        {
            UserKey('-');
        }

        private void ClearX(object sender, RoutedEventArgs e)
        {
            UserKey(',');
        }

        private void EnterExponent(object sender, RoutedEventArgs e)
        {
            UserKey('x');
        }

        private void ChangeSign(object sender, RoutedEventArgs e)
        {
            UserKey('~');
        }

        private void Enter(object sender, RoutedEventArgs e)
        {
            UserKey(' ');
        }

        private void Recall(object sender, RoutedEventArgs e)
        {
            UserKey('<');
        }

        private void Store(object sender, RoutedEventArgs e)
        {
            UserKey('>');
        }

        private void RollDown(object sender, RoutedEventArgs e)
        {
            UserKey('r');
        }

        private void Swap(object sender, RoutedEventArgs e)
        {
            UserKey('w');
        }

        private void Reciprocal(object sender, RoutedEventArgs e)
        {
            UserKey('f');
        }

        private void Tangent(object sender, RoutedEventArgs e)
        {
            UserKey('t');
        }

        private void Cosine(object sender, RoutedEventArgs e)
        {
            UserKey('c');
        }

        private void Sine(object sender, RoutedEventArgs e)
        {
            UserKey('s');
        }

        private void Arc(object sender, RoutedEventArgs e)
        {
            UserKey('a');
            SinButton.BorderBrush = CosButton.BorderBrush = TanButton.BorderBrush = selectedBrush;
        }

        private void SquareRoot(object sender, RoutedEventArgs e)
        {
            UserKey('q');
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            UserKey('!');
        }

        private void Exp(object sender, RoutedEventArgs e)
        {
            UserKey('e');
        }

        private void LogE(object sender, RoutedEventArgs e)
        {
            UserKey('l');
        }

        private void Log10(object sender, RoutedEventArgs e)
        {
            UserKey('g');
        }

        private void Power(object sender, RoutedEventArgs e)
        {
            UserKey('^');
        }

        private bool selectingFunction = false;
        private bool recording = false;
        private string recorded = "";
        private string[] macros = new string[3];

        private int currentMacroIndex = 0;
        private Button currentMacroButton = null;

        private Brush selectedBrush = new SolidColorBrush(Color.FromArgb(255, 0x99, 0, 0));
        private Brush secondaryButtonBrush = new SolidColorBrush(Color.FromArgb(255, 0x70, 0x70, 0x70));

        private void Record(object sender, RoutedEventArgs e)
        {
            buffer.Clear(); // abort pending macros
            if (recording)
            {
                PlayMacro(currentMacroButton, currentMacroIndex); // stop
            }
            else
            {
                selectingFunction = !selectingFunction; // false upon any non-macro-related key
                FButton.BorderBrush = GButton.BorderBrush = HButton.BorderBrush = selectingFunction ? selectedBrush : secondaryButtonBrush;
            }
        }

        private void PlayMacro(Button button, int m)
        {
            if (selectingFunction)
            {
                selectingFunction = false;
                recording = true;
                recorded = "";
                currentMacroIndex = m;
                currentMacroButton = button;
                FButton.BorderBrush = GButton.BorderBrush = HButton.BorderBrush = secondaryButtonBrush;
                button.Background = button.BorderBrush = selectedBrush;
            }
            else if (recording && m == currentMacroIndex)
            {
                recording = false;
                button.Background = button.BorderBrush = secondaryButtonBrush;
                if (recorded.Length > 0) // immediately stopping leaves as is
                    macros[m] = recorded;
            }
            else
            {
                if (macros[m] != null)
                    foreach (var k in macros[m])
                        Key(k);
            }
        }

        private void PlayF(object sender, RoutedEventArgs e)
        {
            PlayMacro(FButton, 0);
        }

        private void PlayG(object sender, RoutedEventArgs e)
        {
            PlayMacro(GButton, 1);
        }

        private void PlayH(object sender, RoutedEventArgs e)
        {
            PlayMacro(HButton, 2);
        }

        private void Page_KeyUp_1(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
                Clear(null, null);
        }

        private void Button_LostFocus_1(object sender, RoutedEventArgs e)
        {
            EnterBtn.Focus(Windows.UI.Xaml.FocusState.Keyboard);
        }
    }
}
