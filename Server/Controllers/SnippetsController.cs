﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BlazorRepl.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnippetsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly BlobContainerClient containerClient;
        public SnippetsController(IConfiguration config)
        {
            _config = config;
            var connectionString = _config.GetConnectionString("StorageConnectionString");
            BlobServiceClient blobServiceClient;

            // AAD Service Principal
            if (connectionString.StartsWith("https"))
                blobServiceClient = new BlobServiceClient(new Uri(connectionString));

            // Connection string and token (local devcelopment
            else
                blobServiceClient = new BlobServiceClient(connectionString);
            containerClient = blobServiceClient.GetBlobContainerClient(_config["SnippetsContainer"]);  
        }
        [HttpGet("{snippetId}")]
        public async Task<IActionResult> Get(string snippetId)
        {
            var blob = containerClient.GetBlobClient(BlobPath(snippetId));
            var response = await blob.DownloadAsync();
            var zipStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(zipStream);
            zipStream.Position = 0;
            return File(zipStream, "application/octet-stream", "snippet.zip");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var newSnippetId = NewSnippetId();
            await containerClient.UploadBlobAsync(BlobPath(newSnippetId), Request.Body);
            return Ok(newSnippetId);
        }

        private static string NewSnippetId()
        {
            var yearFolder = DateTime.Now.Year;
            var monthFolder = DateTime.Now.Month;
            var dayFolder = DateTime.Now.Day;
            var time = Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds);
            var snippetTime = $"{time:D8}";
            return $"{yearFolder:0000}{monthFolder:00}{dayFolder:00}{snippetTime:D8}";
        }

        private static string BlobPath(string snippetId)
        {
            var yearFolder = snippetId.Substring(0, 4);
            var monthFolder = snippetId.Substring(4, 2);
            var dayFolder = snippetId.Substring(6, 2);
            var time = snippetId.Substring(8);
            var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
            var snippetTime = $"{time:00000000}";
            return $"{snippetFolder}/{snippetTime}";
        }
    }
}
