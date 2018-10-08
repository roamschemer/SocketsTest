using Prism.Navigation;
using Reactive.Bindings;
using System;
using System.Linq;
using System.Reactive.Disposables;
using SocketsTest.Models;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace SocketsTest.ViewModels
{
    public class MainPageViewModel : ViewModelBase, IDisposable
    {
        //メモリリーク防止
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();

        //情報
        public ReactiveProperty<bool> ServerSwitch { get; private set; }
        public ReactiveProperty<bool> SettingIsEnabled { get; private set; }
        public ReactiveProperty<string> ServerSwitchInfo { get; private set; }
        public ReactiveProperty<string> IpAddress { get; private set; }
        public ReactiveProperty<bool> IPAddressSettingIsEnabled { get; private set; }
        public ReactiveProperty<int> Port { get; private set; }
        public ReactiveProperty<string> SendData { get; private set; }

        // 挙動情報(エラーや通信履歴)
        public ReadOnlyReactiveCollection<string> ActionView { get; }

        // 開始終了
        public ReactiveCommand OpenCommand { get; private set; }
        public ReactiveCommand CloseCommand { get; private set; }
        public ReactiveCommand SendCommand { get; private set; }

        // Models.Socket
        private Socket socket = new Socket();

        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {

            //ViweModel→Model 
            this.ServerSwitch = ReactiveProperty.FromObject(socket, x => x.IsServer).AddTo(this.Disposable);
            this.IpAddress = ReactiveProperty.FromObject(socket, x => x.IpAddress).AddTo(this.Disposable);
            this.Port = ReactiveProperty.FromObject(socket, x => x.Port).AddTo(this.Disposable);
            //ViewModel←Model
            this.SettingIsEnabled = socket.ObserveProperty(x => x.IsOpen).Select(x => !x).ToReactiveProperty().AddTo(this.Disposable);
            this.CloseCommand = socket.ObserveProperty(x => x.IsOpen).ToReactiveCommand().AddTo(this.Disposable);
            this.OpenCommand = socket.ObserveProperty(x => x.IsOpen).Select(x => !x).ToReactiveCommand().AddTo(this.Disposable);
            this.SendCommand = socket.ObserveProperty(x => x.IsOpen).ToReactiveCommand().AddTo(this.Disposable);
            this.ActionView = socket.ActionInfo.ToReadOnlyReactiveCollection().AddTo(this.Disposable);
            //ViewModel＝Model
            this.SendData = socket.ToReactivePropertyAsSynchronized(x => x.SendData).AddTo(this.Disposable);

            //ViweModel内
            this.ServerSwitchInfo = this.ServerSwitch.Select(x => x ? "サーバー" : "クライアント").ToReactiveProperty().AddTo(this.Disposable);
            this.IPAddressSettingIsEnabled = this.SettingIsEnabled.CombineLatest(this.ServerSwitch, (x, y) => x && !y) //クライアント且つCLOSE時のみIPアドレスは入力編集が可能
                                                 .ToReactiveProperty().AddTo(this.Disposable);

            //ボタン
            OpenCommand.Subscribe(_ => socket.Open());
            CloseCommand.Subscribe(_ => socket.Close());
            SendCommand.Subscribe(_ => socket.Send());

        }

        /// <summary>
        /// メモリリーク防止
        /// </summary>
        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}