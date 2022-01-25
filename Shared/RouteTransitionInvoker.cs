using BlazorTransitionableRoute;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Megaten4Patcher.Shared
{
    public class RouteTransitionInvoker : IRouteTransitionInvoker
    {
        private readonly IJSRuntime jsRuntime;

        public RouteTransitionInvoker(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async Task InvokeRouteTransitionAsync(Transition transition)
        {
            var effectOut = transition.Backwards ? "fadeOutUp" : "fadeOutDown";
            var effectIn = transition.Backwards ? "fadeInUp" : "fadeInDown";

            await jsRuntime.InvokeVoidAsync("window.animJsInterop.transitionFunction", effectOut, effectIn);
        }
    }
}