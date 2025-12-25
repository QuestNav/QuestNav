// QUESTNAV
// https://github.com/QuestNav
// Copyright (C) 2026 QuestNav
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// This file is part of QuestNav.
//
// QuestNav is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// QuestNav is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with QuestNav. If not, see https://www.gnu.org/licenses/.
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestNav.Native.NTCore
{
    public class PolledLogger : IDisposable
    {
        private uint handle;

        public PolledLogger(uint handle)
        {
            this.handle = handle;
        }

        public void Close()
        {
            if (handle != 0)
            {
                NtCoreNatives.NT_DestroyListenerPoller(handle);
                handle = 0;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public unsafe List<(string message, string filename, int line, int level)> PollForMessages()
        {
            if (handle == 0)
            {
                return null;
            }

            UIntPtr len = UIntPtr.Zero;
            NativeNtEvent* events = NtCoreNatives.NT_ReadListenerQueue(handle, &len);

            if (events == null)
            {
                return null;
            }

            try
            {
                List<(string message, string filename, int line, int level)> messages = new List<(
                    string message,
                    string filename,
                    int line,
                    int level
                )>((int)len);
                for (int i = 0; i < (int)len; i++)
                {
                    if (events[i].flags != 0x100)
                    {
                        continue;
                    }

                    string message =
                        (int)events[i].data.logMessage.message.len != 0
                            ? Encoding.UTF8.GetString(
                                events[i].data.logMessage.message.str,
                                (int)events[i].data.logMessage.message.len
                            )
                            : "";
                    string filename =
                        (int)events[i].data.logMessage.filename.len != 0
                            ? Encoding.UTF8.GetString(
                                events[i].data.logMessage.filename.str,
                                (int)events[i].data.logMessage.filename.len
                            )
                            : "";

                    messages.Add(
                        (
                            message,
                            filename,
                            (int)events[i].data.logMessage.line,
                            (int)events[i].data.logMessage.level
                        )
                    );
                }
                return messages;
            }
            finally
            {
                NtCoreNatives.NT_DisposeEventArray(events, len);
            }
        }
    }
}
