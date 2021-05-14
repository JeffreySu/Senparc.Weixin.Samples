using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Weixin.RegisterServices;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Weixin.Sample.Net6.MinSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache()//ʹ�ñ��ػ���������
                    .AddSenparcWeixinServices(Configuration);//Senparc.Weixin ע�ᣨ���룩
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
                IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //ע�� Senparc.Weixin ��������
            var registerService = app.UseSenparcGlobal(env, senparcSetting.Value, _ => { }, true)
                                     .UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister => weixinRegister.RegisterMpAccount(senparcWeixinSetting.Value));

            app.UseRouting();

            //ʹ���м��ע�� MessageHandler��ָ�� CustomMessageHandler Ϊ�Զ��崦����
            app.UseMessageHandlerForMp("/WeixinAsync",
                (stream, postModel, maxRecordCount, serviceProvider) => new CustomMessageHandler(stream, postModel, maxRecordCount, serviceProvider),
                options =>
                {
                    options.AccountSettingFunc = context => senparcWeixinSetting.Value;
                });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Open /WeixinAsync to connect WeChat MessageHandler");//��ҳĬ����ʾ
                });
            });
        }
    }

    /// <summary>
    /// �Զ�����Ϣ������
    /// </summary>
    public class CustomMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, IServiceProvider serviceProvider = null)
            : base(inputStream, postModel, maxRecordCount, false, null, serviceProvider)
        {
        }

        /// <summary>
        /// �ظ���������ʽ���͵���Ϣ����ѡ��
        /// </summary>
        public override async Task<IResponseMessageBase> OnTextOrEventRequestAsync(RequestMessageText requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            await MP.AdvancedAPIs.CustomApi.SendTextAsync(Config.SenparcWeixinSetting.MpSetting.WeixinAppId, OpenId, $"����һ���첽�Ŀͷ���Ϣ");//ע�⣺ֻ�в��ԺŻ�����ʽ��������ʽ����ſ��ô˽ӿ�
            responseMessage.Content = $"�㷢�������֣�{requestMessage.Content}\r\n\r\n���OpenId��{OpenId}";//������������Ϣ�ظ�
            return responseMessage;
        }

        /// <summary>
        /// Ĭ����Ϣ
        /// </summary>
        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = $"��ӭ�����ҵĹ��ںţ���ǰʱ�䣺{SystemTime.Now}";//û���Զ������Ϣͳһ�ظ��̶���Ϣ
            return responseMessage;
        }
    }
}
