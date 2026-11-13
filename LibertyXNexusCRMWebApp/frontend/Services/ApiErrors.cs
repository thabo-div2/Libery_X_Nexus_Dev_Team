namespace frontend.Services
{
    /// <summary>
    /// Turns a failed API reply into a message that is safe to show on the page.
    /// </summary>
    public static class ApiErrors
    {
        public const string CantReachServer = "Can't reach the server. Please try again.";

        public const string Unexpected = "Something went wrong. Please try again.";

        /// <summary>
        /// Pulls the API's own message out of an error reply, e.g. { "message": "Choose a valid date." }.
        /// Falls back to a plain message when there isn't one.
        /// </summary>
        public static async Task<string> ReadAsync(HttpResponseMessage response)
        {
            var status = (int)response.StatusCode;

            if (status == 401 || status == 403)
            {
                return "You don't have permission to do that. Please sign in again.";
            }

            if (status == 429)
            {
                return "Too many requests. Please wait a moment and try again.";
            }

            try
            {
                var body = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var json = System.Text.Json.JsonDocument.Parse(body);
                    var root = json.RootElement;

                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("message", out var message)
                            && message.ValueKind == System.Text.Json.JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(message.GetString()))
                        {
                            return message.GetString()!;
                        }

                        // Validation errors look like { "errors": { "Note": [ "..." ] } }
                        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var field in errors.EnumerateObject())
                            {
                                if (field.Value.ValueKind != System.Text.Json.JsonValueKind.Array)
                                {
                                    continue;
                                }

                                foreach (var item in field.Value.EnumerateArray())
                                {
                                    if (item.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                                    {
                                        return item.GetString()!;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Not JSON, so fall through to the plain message below.
            }
            catch (Exception)
            {
                // The reply couldn't be read at all.
            }

            return $"The server reported an error (status {status}).";
        }

        /// <summary>
        /// Sends a request and reads a JSON reply, turning every kind of failure into a message.
        /// </summary>
        public static async Task<(T? Value, string? Error)> SendAsync<T>(Func<Task<HttpResponseMessage>> send) where T : class
        {
            try
            {
                var response = await send();

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadAsync(response));
                }

                var value = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(response.Content);

                return value is null
                    ? (null, "The server sent back an empty reply. Please try again.")
                    : (value, null);
            }
            catch (HttpRequestException)
            {
                return (null, CantReachServer);
            }
            catch (Exception)
            {
                return (null, Unexpected);
            }
        }

        /// <summary>
        /// Sends a request that has no reply body. Returns an error message, or null if it worked.
        /// </summary>
        public static async Task<string?> SendAsync(Func<Task<HttpResponseMessage>> send)
        {
            try
            {
                var response = await send();
                return response.IsSuccessStatusCode ? null : await ReadAsync(response);
            }
            catch (HttpRequestException)
            {
                return CantReachServer;
            }
            catch (Exception)
            {
                return Unexpected;
            }
        }
    }

    /// <summary>
    /// Readable names for document types: "ProofOfAddress" becomes "Proof of address".
    /// </summary>
    public static class DocumentTypeNames
    {
        public static string Format(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "Document";
            }

            if (type == "CertifiedIdCopy")
            {
                return "Certified ID copy";
            }

            var spaced = System.Text.RegularExpressions.Regex.Replace(type, "(?<!^)([A-Z])", " $1").ToLowerInvariant();
            return char.ToUpperInvariant(spaced[0]) + spaced[1..];
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
