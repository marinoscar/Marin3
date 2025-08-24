using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Agents
{
    public static class ChatMessageContentUsageExtensions
    {
        /// <summary>
        /// Tries to extract token usage (input/output/total) from a ChatMessageContent
        /// across multiple SK connectors (OpenAI/Azure OpenAI, Mistral, Bedrock/Anthropic, Meta Llama via various backends).
        /// Returns null if unavailable.
        /// </summary>
        public static LlmUsage? TryGetUsage(this ChatMessageContent reply)
        {
            if (reply is null) return null;

            // 1) Strongly-typed OpenAI v2 path via InnerContent
            //    SK team recommends using InnerContent for OpenAI-compatible endpoints. 
            //    (Usage: OpenAI.Chat.ChatCompletion -> .Usage.*TokenCount)
            //    Ref: GH discussion “How to correctly obtain tokens usage…” (Feb 2025).
            var inner = reply.InnerContent;
            var usage = FromOpenAIInnerContent(inner);
            if (usage is not null)
                return usage;

            // 2) Generic Metadata["Usage"] path (OpenAI & Mistral often place usage here)
            if (reply.Metadata is IReadOnlyDictionary<string, object?> meta &&
                meta.TryGetValue("Usage", out var usageObj) && usageObj is not null)
            {
                // 2a) OpenAI.Chat.ChatTokenUsage
                var fromOpenAiMeta = FromOpenAIUsageObject(usageObj);
                if (fromOpenAiMeta is not null) return fromOpenAiMeta;

                // 2b) Mistral or other shapes (prompt/completion/total tokens style)
                var fromGeneric = FromGenericUsageObject(usageObj);
                if (fromGeneric is not null) return fromGeneric;
            }

            // 3) Some connectors don’t currently surface usage (e.g., Bedrock/Anthropic via SK)
            //    We return null so callers can fallback to provider-native telemetry.
            //    Ref: issue noting Bedrock connector doesn’t populate usage in Metadata.
            return null;
        }

        // ---------- OpenAI helpers ----------

        private static LlmUsage? FromOpenAIInnerContent(object? inner)
        {
            // Reflection to avoid hard dependency on OpenAI .NET v2 types at compile-time
            // Expected: OpenAI.Chat.ChatCompletion with property "Usage" of type ChatTokenUsage
            if (inner is null) return null;
            var t = inner.GetType();
            if (t.FullName is not null && t.FullName.Contains("OpenAI.Chat.ChatCompletion", StringComparison.OrdinalIgnoreCase))
            {
                var usageProp = t.GetProperty("Usage", BindingFlags.Public | BindingFlags.Instance);
                var usageVal = usageProp?.GetValue(inner);
                var parsed = FromOpenAIUsageObject(usageVal);
                if (parsed is not null) return parsed;
            }
            return null;
        }

        private static LlmUsage? FromOpenAIUsageObject(object? usageObj)
        {
            if (usageObj is null) return null;
            var ut = usageObj.GetType();

            // Properties in OpenAI v2: InputTokenCount, OutputTokenCount, TotalTokenCount
            int? input = GetIntProp(ut, usageObj, "InputTokenCount");
            int? output = GetIntProp(ut, usageObj, "OutputTokenCount");
            int? total = GetIntProp(ut, usageObj, "TotalTokenCount");

            if (input is not null || output is not null || total is not null)
            {
                return new LlmUsage
                {
                    InputTokens = input ?? 0,
                    OutputTokens = output ?? 0,
                    TotalTokens = (total ?? (input + output)) ?? 0
                };
            }
            return null;
        }

        // ---------- Generic / Mistral-like helpers ----------

        private static LlmUsage? FromGenericUsageObject(object usageObj)
        {
            var t = usageObj.GetType();

            // Try common property name patterns across non-OpenAI providers
            // Mistral / HF-style often: PromptTokens / CompletionTokens / TotalTokens
            // or prompt_tokens / completion_tokens / total_tokens (JSON -> POCO)
            int? input =
                GetIntProp(t, usageObj, "PromptTokens")
                ?? GetIntProp(t, usageObj, "prompt_tokens")
                ?? GetIntProp(t, usageObj, "InputTokens")
                ?? GetIntProp(t, usageObj, "input_tokens");

            int? output =
                GetIntProp(t, usageObj, "CompletionTokens")
                ?? GetIntProp(t, usageObj, "completion_tokens")
                ?? GetIntProp(t, usageObj, "OutputTokens")
                ?? GetIntProp(t, usageObj, "output_tokens");

            int? total =
                GetIntProp(t, usageObj, "TotalTokens")
                ?? GetIntProp(t, usageObj, "total_tokens");

            if (input is null && output is null && total is null)
            {
                // As a last resort, look for any ints named “tokens”
                var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.PropertyType == typeof(int) &&
                                         p.Name.Contains("token", StringComparison.OrdinalIgnoreCase))
                             .ToList();
                if (props.Count == 1) total = (int?)props[0].GetValue(usageObj);
            }

            if (input is not null || output is not null || total is not null)
            {
                return new LlmUsage
                {
                    InputTokens = input ?? 0,
                    OutputTokens = output ?? 0,
                    TotalTokens = (total ?? (input + output)) ?? 0
                };
            }

            return null;
        }

        private static int? GetIntProp(Type t, object instance, string name)
            => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance) as int?;

    }
}
