﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;


using XperienceAdapter.Models;
using Business.Models;

namespace Business.Services
{
    public class FileService : IFileService
    {
        // If you require a check on specific characters in the IsValidFileExtensionAndSignature
        // method, supply the characters in the _allowedChars field.
        private static readonly byte[] _allowedChars = { };

        // For more file signatures, see the File Signatures Database (https://www.filesignatures.net/)
        // and the official specifications for the file types you wish to add.
        private static readonly Dictionary<string, List<byte[]>> _fileSignature = new Dictionary<string, List<byte[]>>
        {
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".jpeg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
                }
            },
            { ".jpg", new List<byte[]>
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
                }
            },
            { ".zip", new List<byte[]>
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                    new byte[] { 0x50, 0x4B, 0x4C, 0x49, 0x54, 0x45 },
                    new byte[] { 0x50, 0x4B, 0x53, 0x70, 0x58 },
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 },
                    new byte[] { 0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 },
                }
            },
        };

        public async Task<(FormFileResultState ResultState, UploadedFile? UploadedFile)> ProcessFormFile(IFormFile formFile,
            string[] permittedExtensions,
            long sizeLimit = 4194304)
        {
            // Don't trust the file name sent by the client. To display
            // the file name, HTML-encode the value.
            var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                formFile.FileName);

            // Check the file length. This check doesn't catch files that only have 
            // a BOM as their content.
            if (formFile.Length == 0)
            {
                return (FormFileResultState.FileEmpty, null);
            }

            if (formFile.Length > sizeLimit)
            {
                return (FormFileResultState.FileTooBig, null);
            }

            using (var memoryStream = new MemoryStream())
            {
                await formFile.CopyToAsync(memoryStream);

                // Check the content length in case the file's only
                // content was a BOM and the content is actually
                // empty after removing the BOM.
                if (memoryStream.Length == 0)
                {
                    return (FormFileResultState.FileEmpty, null);
                }

                if (!IsValidFileExtensionAndSignature(
                    formFile.FileName, memoryStream, permittedExtensions))
                {
                    return (FormFileResultState.ForbiddenFileType, null);
                }
                else
                {
                    return (FormFileResultState.FileOk, new UploadedFile(formFile));
                }
            }
        }

        private static bool IsValidFileExtensionAndSignature(string fileName, Stream data, string[] permittedExtensions)
        {
            if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
            {
                return false;
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            {
                return false;
            }

            data.Position = 0;

            using (var reader = new BinaryReader(data))
            {
                if (ext.Equals(".txt") || ext.Equals(".csv") || ext.Equals(".prn"))
                {
                    if (_allowedChars.Length == 0)
                    {
                        // Limits characters to ASCII encoding.
                        for (var i = 0; i < data.Length; i++)
                        {
                            if (reader.ReadByte() > sbyte.MaxValue)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // Limits characters to ASCII encoding and
                        // values of the _allowedChars array.
                        for (var i = 0; i < data.Length; i++)
                        {
                            var b = reader.ReadByte();
                            if (b > sbyte.MaxValue ||
                                !_allowedChars.Contains(b))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                // File signature check
                // --------------------
                // With the file signatures provided in the _fileSignature
                // dictionary, the following code tests the input content's
                // file signature.
                var signatures = _fileSignature[ext];
                var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

                return signatures.Any(signature =>
                    headerBytes.Take(signature.Length).SequenceEqual(signature));
            }
        }
    }
}
