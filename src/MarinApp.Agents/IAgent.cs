using MarinApp.Agents.Data;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarinApp.Agents
{
    /// <summary>
    /// Defines the contract for conversational agents that interact with users via chat, manage session history,
    /// and interface with a semantic kernel for message completion and streaming.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of <see cref="IAgent"/> are responsible for managing chat sessions, handling system prompts,
    /// sending and streaming messages, and maintaining chat history. The interface supports both synchronous and
    /// asynchronous message processing, as well as event notification for message completion.
    /// </para>
    /// <para>
    /// <b>Session Management:</b> Use <see cref="StartSession"/> to begin a new session, or <see cref="SetSession"/> to resume an existing one.
    /// The <see cref="RestoreHistoryAsync"/> method loads previous messages for a session.
    /// </para>
    /// <para>
    /// <b>Message Handling:</b> Use <see cref="SendMessageAsync"/> for single-turn responses and <see cref="StreamMessageAsync"/> for streaming responses.
    /// Both methods support sending messages as plain text, templates with data, or <see cref="ChatMessageContent"/> objects.
    /// </para>
    /// <para>
    /// <b>System Prompts:</b> Set the system prompt using <see cref="SetSystemMessage"/> or <see cref="SetSystemMessage{T}"/> for templated prompts.
    /// </para>
    /// <para>
    /// <b>Events:</b> Subscribe to <see cref="MessageCompleted"/> to handle message completion events, which provide both the message content and the persisted <see cref="AgentMessage"/> entity.
    /// </para>
    /// </remarks>
    public interface IAgent
    {
        /// <summary>
        /// Gets or sets the description of the agent.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets the chat history for the current session.
        /// </summary>
        ChatHistory History { get; }

        /// <summary>
        /// Gets or sets the unique identifier for the agent.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the agent.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the current system prompt for the agent.
        /// </summary>
        string SystemPrompt { get; }

        /// <summary>
        /// Gets or sets the default execution settings for prompt operations.
        /// </summary>
        PromptExecutionSettings DefaultExecutionSettings { get; set; }

        /// <summary>
        /// Occurs when a message has been completed and processed by the agent.
        /// </summary>
        /// <remarks>
        /// The event provides both the <see cref="ChatMessageContent"/> and the persisted <see cref="AgentMessage"/> entity.
        /// </remarks>
        event EventHandler<(ChatMessageContent MessageContent, AgentMessage AgentMessage)>? MessageCompleted;

        /// <summary>
        /// Restores the chat history for the specified session from persistent storage.
        /// </summary>
        /// <param name="sessionId">The session identifier to restore.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RestoreHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to the agent using a <see cref="ChatMessageContent"/> object, returning the agent's response.
        /// </summary>
        /// <param name="content">The user message content.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> SendMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to the agent using a prompt string, returning the agent's response.
        /// </summary>
        /// <param name="prompt">The prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> SendMessageAsync(string prompt, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to the agent using a Handlebars template and data, returning the agent's response.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> SendMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the current session to the specified session ID and resets the chat history.
        /// </summary>
        /// <param name="sessionId">The session identifier to set.</param>
        /// <returns>The session identifier that was set.</returns>
        string SetSession(string sessionId);

        /// <summary>
        /// Sets the system prompt to the specified message.
        /// </summary>
        /// <param name="message">The system prompt message.</param>
        void SetSystemMessage(string message);

        /// <summary>
        /// Sets the system prompt using a Handlebars template and data model.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        void SetSystemMessage<T>(string template, T data);

        /// <summary>
        /// Starts a new session and resets the chat history.
        /// </summary>
        /// <returns>The new session identifier.</returns>
        string StartSession();

        /// <summary>
        /// Streams a message to the agent using a <see cref="ChatMessageContent"/> object, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="content">The user message content.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> StreamMessageAsync(ChatMessageContent content, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams a message to the agent using a prompt string, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <param name="prompt">The prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> StreamMessageAsync(string prompt, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams a message to the agent using a Handlebars template and data, invoking a callback for each streaming response chunk.
        /// </summary>
        /// <typeparam name="T">The type of the data model.</typeparam>
        /// <param name="template">The Handlebars template string.</param>
        /// <param name="data">The data model to apply to the template.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="onResponse">Callback invoked for each streaming response chunk.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>The final <see cref="AgentMessage"/> response.</returns>
        Task<AgentMessage> StreamMessageAsync<T>(string template, T data, PromptExecutionSettings executionSettings, Action<StreamingChatMessageContent> onResponse, CancellationToken cancellationToken = default);
    }
}