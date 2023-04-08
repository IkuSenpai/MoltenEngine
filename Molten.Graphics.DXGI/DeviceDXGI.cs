﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Molten.Graphics.Dxgi
{
    public abstract unsafe class DeviceDXGI : GraphicsDevice
    {
        /// <summary>Gets the native DXGI adapter that this instance represents.</summary>
        public IDXGIAdapter4* _adapter;
        AdapterDesc3* _adapterDesc;
        List<DisplayOutputDXGI> _outputs;
        List<DisplayOutputDXGI> _activeOutputs;

        protected DeviceDXGI(RenderService renderer, GraphicsManagerDXGI manager, IDXGIAdapter4* adapter) : 
            base(renderer, manager)
        {
            _adapter = adapter;
            Capabilities = new GraphicsCapabilities();

            _adapterDesc = EngineUtil.Alloc<AdapterDesc3>();
            adapter->GetDesc3(_adapterDesc);
            ID = (DeviceID)_adapterDesc->AdapterLuid;

            Name = SilkMarshal.PtrToString((nint)_adapterDesc->Description, NativeStringEncoding.LPWStr);
            Vendor = EngineUtil.VendorFromPCI(_adapterDesc->VendorId);

            Capabilities.DedicatedSystemMemory = ByteMath.ToMegabytes(_adapterDesc->DedicatedSystemMemory);
            Capabilities.DedicatedVideoMemory = ByteMath.ToMegabytes(_adapterDesc->DedicatedVideoMemory);

            nuint sharedMemory = _adapterDesc->SharedSystemMemory;
            sharedMemory = sharedMemory < 0 ? 0 : sharedMemory;
            Capabilities.SharedSystemMemory = ByteMath.ToMegabytes(sharedMemory);
            Type = GetAdapterType(Capabilities, _adapterDesc->Flags);

            IDXGIOutput1*[] dxgiOutputs = DXGIHelper.EnumArray<IDXGIOutput1, IDXGIOutput>((uint index, ref IDXGIOutput* ptrOutput) =>
            {
                return adapter->EnumOutputs(index, ref ptrOutput);
            });

            _activeOutputs = new List<DisplayOutputDXGI>();
            ActiveOutputs = _activeOutputs.AsReadOnly();
            _outputs = new List<DisplayOutputDXGI>();
            Outputs = _outputs.AsReadOnly();
            Guid o6Guid = IDXGIOutput6.Guid;

            for (int i = 0; i < dxgiOutputs.Length; i++)
            {
                void* ptr6 = null;
                int r = dxgiOutputs[i]->QueryInterface(&o6Guid, &ptr6);

                if (DXGIHelper.ErrorFromResult(r) != DxgiError.Ok)
                    manager.Log.Error($"Error while querying adapter '{Name}' output IDXGIOutput1 for IDXGIOutput6 interface");

                _outputs.Add(new DisplayOutputDXGI(this, (IDXGIOutput6*)ptr6));
            }
        }

        private GraphicsDeviceType GetAdapterType(GraphicsCapabilities cap, AdapterFlag3 flags)
        {
            if ((flags & AdapterFlag3.Software) == AdapterFlag3.Software)
                return GraphicsDeviceType.Cpu;

            if (cap.DedicatedVideoMemory > 0)
                return GraphicsDeviceType.DiscreteGpu;

            if (cap.DedicatedSystemMemory > 0 || cap.SharedSystemMemory > 0)
                return GraphicsDeviceType.IntegratedGpu;

            return GraphicsDeviceType.Other;
        }

        public override void AddActiveOutput(IDisplayOutput output)
        {
            if (output.Device != this)
                throw new DisplayOutputException(output, "Cannot add active output: Bound to another adapter.");

            if (!_activeOutputs.Contains(output))
            {
                _activeOutputs.Add(output as DisplayOutputDXGI);
                InvokeOutputActivated(output);
            }
        }

        public override void RemoveActiveOutput(IDisplayOutput output)
        {
            if (output.Device != this)
                throw new DisplayOutputException(output, "Cannot remove active output: Bound to another adapter.");

            if (_activeOutputs.Remove(output as DisplayOutputDXGI))
                InvokeOutputDeactivated(output);
        }

        public override void RemoveAllActiveOutputs()
        {
            for (int i = 0; i < _activeOutputs.Count; i++)
                InvokeOutputDeactivated(_activeOutputs[i]);

            _activeOutputs.Clear();
        }

        protected override void OnDispose()
        {
            EngineUtil.Free(ref _adapterDesc);
            SilkUtil.ReleasePtr(ref _adapter);
        }

        /// <<inheritdoc/>
        public override DeviceID ID { get;  }

        /// <inheritdoc/>
        public override DeviceVendor Vendor { get;  }

        /// <inheritdoc/>
        public override GraphicsDeviceType Type { get; }

        internal AdapterDesc3* Description => _adapterDesc;

        /// <inheritdoc/>
        public override IReadOnlyList<IDisplayOutput> Outputs { get; }

        /// <inheritdoc/>
        public override IReadOnlyList<IDisplayOutput> ActiveOutputs { get; }

        public IDXGIAdapter4* Adapter => _adapter;
    }
}