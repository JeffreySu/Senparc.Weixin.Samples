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
            services.AddMemoryCache()//使用本地缓存必须添加
                    .AddSenparcWeixinServices(Configuration);//Senparc.Weixin 注册（必须）
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
                IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //注册 Senparc.Weixin 及基础库
            var registerService = app.UseSenparcGlobal(env, senparcSetting.Value, _ => { }, true)
                                     .UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister => weixinRegister.RegisterMpAccount(senparcWeixinSetting.Value));

            app.UseRouting();

            //使用中间件注册 MessageHandler，指定 CustomMessageHandler 为自定义处理方法
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
                    await context.Response.WriteAsync("Open /WeixinAsync to connect WeChat MessageHandler");//首页默认显示
                });
            });
        }
    }

    /// <summary>
    /// 自定义消息处理器
    /// </summary>
    public class CustomMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, IServiceProvider serviceProvider = null)
            : base(inputStream, postModel, maxRecordCount, false, null, serviceProvider)
        {
        }

        /// <summary>
        /// 回复以文字形式发送的信息（可选）
        /// </summary>
        public override async Task<IResponseMessageBase> OnTextOrEventRequestAsync(RequestMessageText requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            await MP.AdvancedAPIs.CustomApi.SendTextAsync(Config.SenparcWeixinSetting.MpSetting.WeixinAppId, OpenId, $"这是一条异步的客服消息");//注意：只有测试号或部署到正式环境的正式服务号可用此接口
            responseMessage.Content = $"你发送了文字：{requestMessage.Content}\r\n\r\n你的OpenId：{OpenId}";//以文字类型消息回复
            return responseMessage;
        }

        /// <summary>
        /// 默认消息
        /// </summary>
        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = $"欢迎来到我的公众号！当前时间：{SystemTime.Now}";//没有自定义的消息统一回复固定消息
            return responseMessage;
        }
    }
}
