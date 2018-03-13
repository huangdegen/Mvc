using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace MvcSandbox
{
    public class CustomPageHandlerMethodSelector : IPageHandlerMethodSelector
    {
        private const string Handler = "handler";

        public HandlerMethodDescriptor Select(PageContext context)
        {
            var handlers = SelectHandlers(context);
            if (handlers == null || handlers.Count == 0)
            {
                return null;
            }

            List<HandlerMethodDescriptor> ambiguousMatches = null;
            HandlerMethodDescriptor bestMatch = null;
            for (var score = 4; score >= 0; score--)
            {
                for (var i = 0; i < handlers.Count; i++)
                {
                    var handler = handlers[i];
                    if (GetScore(handler, context.HttpContext.Request.Method) == score)
                    {
                        if (bestMatch == null)
                        {
                            bestMatch = handler;
                            continue;
                        }

                        if (ambiguousMatches == null)
                        {
                            ambiguousMatches = new List<HandlerMethodDescriptor>();
                            ambiguousMatches.Add(bestMatch);
                        }

                        ambiguousMatches.Add(handler);
                    }
                }

                if (ambiguousMatches != null)
                {
                    throw new InvalidOperationException("It's ambiguous YO.");
                }

                if (bestMatch != null)
                {
                    return bestMatch;
                }
            }

            return null;
        }

        private static List<HandlerMethodDescriptor> SelectHandlers(PageContext context)
        {
            var handlers = context.ActionDescriptor.HandlerMethods;
            List<HandlerMethodDescriptor> handlersToConsider = null;

            var handlerName = Convert.ToString(context.RouteData.Values[Handler]);

            if (string.IsNullOrEmpty(handlerName) &&
                context.HttpContext.Request.Query.TryGetValue(Handler, out StringValues queryValues))
            {
                handlerName = queryValues[0];
            }

            // Supports fuzzy matching on verbs, for instance HEAD can call OnGetAsync
            var httpMethodCategory = GetHttpMethodCategory(context.HttpContext.Request.Method);

            for (var i = 0; i < handlers.Count; i++)
            {
                var handler = handlers[i];
                if (handler.HttpMethod != null &&
                    !string.Equals(handler.HttpMethod, context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(handler.HttpMethod, httpMethodCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (handler.Name != null &&
                    !handler.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (handlersToConsider == null)
                {
                    handlersToConsider = new List<HandlerMethodDescriptor>();
                }

                handlersToConsider.Add(handler);
            }

            return handlersToConsider;
        }

        private static string GetHttpMethodCategory(string httpMethod)
        {
            if (string.Equals("GET", httpMethod, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("HEAD", httpMethod, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("OPTIONS", httpMethod, StringComparison.OrdinalIgnoreCase))
            {
                return "GET";
            }

            return "POST";
        }

        // Consider a case like 

        private static int GetScore(HandlerMethodDescriptor descriptor, string httpMethod)
        {
            var score = 0;

            if (descriptor.Name != null)
            {
                // Handler name match. This means that the HTTP method is compatible.
                score += 1;
            }

            if (descriptor.HttpMethod != null && 
                string.Equals(descriptor.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase))
            {
                // Exact HTTP method match.
                score += 3;
            }
            else if (descriptor.HttpMethod != null)
            {
                // Fuzzy HTTP method match.
                score += 1;
            }

            return score;
        }
    }
}
