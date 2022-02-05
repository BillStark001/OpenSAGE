﻿using System.IO;
using OpenAS2.Script;

namespace OpenAS2.Script
{
    /// <summary>
    /// Url handler
    /// </summary>
    public static class UrlHandler
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static void Handle(VM.HandleCommand cmdHandler, VM.HandleExternalMovie movieHandler, ExecutionContext context, string url, string target)
        {
            logger.Debug($"[URL] URL: {url} Target: {target}");

            if (url.StartsWith("FSCommand:"))
            {
                var command = url.Replace("FSCommand:", "");

                cmdHandler(context, command, target);
            }
            else
            {
                //DO STUFF
                var targetObject = context.This.GetMember(target).ToObject<StageObject>();

                if (!(targetObject.Item is SpriteItem))
                {
                    logger.Error("[URL] Target must be a sprite!");
                }

                var targetSprite = targetObject.Item as SpriteItem;
                var aptFile = movieHandler(url);
                var oldName = targetSprite.Name;

                targetSprite.Create(aptFile.Movie, targetSprite.Context, targetSprite.Parent);
                targetSprite.Name = oldName;
            }
        }
    }
}
