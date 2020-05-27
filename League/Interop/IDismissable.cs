﻿using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRrhythmLeague.Interop
{
    internal interface IDismissable
    {
        FlowCoordinator ParentFlowCoordinator { get; set; }
        void Dismiss(bool immediately);
    }
}
