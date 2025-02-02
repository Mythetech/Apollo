using Apollo.Components.DynamicTabs;
using Microsoft.AspNetCore.Components.Rendering;

namespace Apollo.Test.Components.DynamicTabs;

public class TestDynamicTabs
{
    private static void BuildTestTab(RenderTreeBuilder builder, string name)
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, $"<h1>{name}</h1>");
        builder.CloseElement();
    }
    
    public class TestDynamicTab1 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab1";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab1);

        public override string DefaultArea { get; } = DropZones.Left;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
    
    public class TestDynamicTab2 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab2";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab2);

        public override string DefaultArea { get; } = DropZones.Right;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
    
    public class TestDynamicTab3 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab3";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab3);

        public override string DefaultArea { get; } = DropZones.Bottom;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
    
    public class TestDynamicTab4 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab4";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab4);

        public override string DefaultArea { get; } = DropZones.Docked;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
    
    public class TestDynamicTab5 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab5";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab5);

        public override string DefaultArea { get; } = DropZones.Floating;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
    
    public class TestDynamicTab6 : DynamicTabView
    {
        public override string Name { get; set; } = "TestDynamicTab6";
        public override Type ComponentType { get; set; } = typeof(TestDynamicTab6);

        public override string DefaultArea { get; } = DropZones.None;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            BuildTestTab(builder, Name);
            base.BuildRenderTree(builder);
        }
    }
}