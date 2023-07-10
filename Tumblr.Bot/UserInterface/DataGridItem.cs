using System.ComponentModel;
using System.Runtime.CompilerServices;
using Waifu.WPF.Properties;

namespace Tumblr.Bot.UserInterface
{
    public class DataGridItem : INotifyPropertyChanged
    {

        private string _status;
        private string _account;
        private long _greets;
        private long _in;
        private long _out;

        public string Account
        {
            get => _account;
            set
            {
                if (_account == value)
                    return;

                _account = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value)
                    return;

                _status = value;
                OnPropertyChanged();
            }
        }

        public long GreetCnt
        {
            get => _greets;
            set
            {
                _greets = value;
                OnPropertyChanged(nameof(Greets));
            }
        }

        public long InCnt
        {
            get => _in;
            set
            {
                _in = value;
                OnPropertyChanged(nameof(In));
            }
        }

        public long OutCnt
        {
            get => _out;
            set
            {
                _out = value;
                OnPropertyChanged(nameof(Out));
            }
        }

        public string Greets => _greets.ToString("N0");
        public string In => _in.ToString("N0");
        public string Out => _out.ToString("N0");

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
