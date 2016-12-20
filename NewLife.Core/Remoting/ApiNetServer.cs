﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiNetServer : NetServer<ApiNetSession>, IApiServer
    {
        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        public ApiNetServer()
        {
            Name = "Api";
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public bool Init(string config)
        {
            Local = new NetUri(config);
#if DEBUG
            //LogSend = true;
            //LogReceive = true;
#endif

            return true;
        }
    }

    class ApiNetSession : NetSession<ApiNetServer>, IApiSession
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            var enc = Host.Encoder;

            var act = "";
            IDictionary<string, object> args = null;
            if (enc.Decode(e.Data, out act, out args))
            {
                OnInvoke(act, args);
            }
        }

        /// <summary>处理远程调用</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        protected virtual async void OnInvoke(string action, IDictionary<string, object> args)
        {
            var enc = Host.Encoder;
            object result = null;
            var rs = false;
            try
            {
                result = await Host.Handler.Execute(this, action, args);

                rs = true;
            }
            catch (Exception ex)
            {
                //result = ex.Message;
                result = ex;
            }

            var buf = enc.Encode(rs, result);

            Session.Send(buf);

            //var task = Host.Handler.Execute(this, action, args);
            //if (task == null) return;

            //task.ContinueWith(t =>
            //{
            //    var rs = t.IsOK();
            //    var result = rs ? t.Result : t.Exception;

            //    var buf = Host.Encoder.Encode(rs, result);

            //    Session.Send(buf);
            //});
        }

        /// <summary>远程调用</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<TResult> Invoke<TResult>(string action, object args = null)
        {
            var enc = Host.Encoder;
            var data = enc.Encode(action, args);

            var rs = await SendAsync(data);

            return enc.Decode<TResult>(rs);
        }
    }
}
