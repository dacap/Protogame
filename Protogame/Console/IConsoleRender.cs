﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Protogame
{
    public interface IConsoleRender
    {
        void Render(IGameContext gameContext, IRenderContext renderContext, StringBuilder inputBuffer, ConsoleState state, List<Tuple<ConsoleLogLevel, string>> logEntries);
    }
}
