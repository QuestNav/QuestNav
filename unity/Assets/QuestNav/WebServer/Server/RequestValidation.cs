using System;
using QuestNav.Core;

namespace QuestNav.WebServer.Server
{
    /// <summary>
    /// Request checks used by <see cref="ConfigServer"/>, kept free of EmbedIO types so they
    /// can be unit tested.
    /// </summary>
    public static class RequestValidation
    {
        /// <summary>
        /// Returns true if a state-changing request may proceed. A missing Origin header
        /// means a non-browser client such as curl. A present one must name the same host
        /// and port as the Host header, so a page from another origin (or an opaque "null"
        /// origin) cannot change the headset's state.
        /// </summary>
        /// <param name="origin">The Origin header, or null if absent.</param>
        /// <param name="host">The Host header.</param>
        public static bool IsAllowedOrigin(string origin, string host)
        {
            if (origin == null)
            {
                return true;
            }
            if (
                string.IsNullOrWhiteSpace(host)
                || !Uri.TryCreate(origin, UriKind.Absolute, out Uri originUri)
                || (originUri.Scheme != Uri.UriSchemeHttp && originUri.Scheme != Uri.UriSchemeHttps)
                || !Uri.TryCreate(
                    $"{originUri.Scheme}://{host.Trim()}",
                    UriKind.Absolute,
                    out Uri hostUri
                )
            )
            {
                return false;
            }
            // Authority drops a default port, so "http://quest" matches Host "quest:80".
            return string.Equals(
                originUri.Authority,
                hostUri.Authority,
                StringComparison.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Returns true if the Content-Type header names one of the accepted media types.
        /// Parameters such as "; charset=utf-8" are ignored.
        /// </summary>
        public static bool HasMediaType(string contentType, params string[] accepted)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }
            int separator = contentType.IndexOf(';');
            string mediaType = (
                separator >= 0 ? contentType.Substring(0, separator) : contentType
            ).Trim();
            foreach (var candidate in accepted)
            {
                if (string.Equals(mediaType, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks an ignored-ID list: at most <see cref="QuestNavConstants.AprilTag.MAX_IGNORED_IDS"/>
        /// entries, each a valid tag36h11 ID. A null list is valid (nothing ignored).
        /// </summary>
        /// <param name="ids">The IDs from the request.</param>
        /// <param name="error">Why the list was rejected, or null.</param>
        public static bool TryValidateIgnoredIds(int[] ids, out string error)
        {
            error = null;
            if (ids == null)
            {
                return true;
            }
            if (ids.Length > QuestNavConstants.AprilTag.MAX_IGNORED_IDS)
            {
                error =
                    $"ignoredIds has {ids.Length} entries; at most "
                    + $"{QuestNavConstants.AprilTag.MAX_IGNORED_IDS} are allowed.";
                return false;
            }
            foreach (int id in ids)
            {
                if (id < 0 || id > QuestNavConstants.AprilTag.MAX_TAG36H11_ID)
                {
                    error =
                        $"ignoredIds contains {id}; tag IDs must be between 0 and "
                        + $"{QuestNavConstants.AprilTag.MAX_TAG36H11_ID}.";
                    return false;
                }
            }
            return true;
        }
    }
}
