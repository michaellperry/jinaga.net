﻿using FluentAssertions;
using Jinaga.Facts;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jinaga.Test.Facts
{
    public class FactGraphBuilderTest
    {
        [Fact]
        public void Builder_EmptyFactGraph()
        {
            var builder = new FactGraphBuilder();
            var factGraph = builder.Build();
            Assert.Empty(factGraph.FactReferences);
        }

        [Fact]
        public void Builder_SingleFact()
        {
            var builder = new FactGraphBuilder();
            builder.Add(Fact.Create(
                "Test.Root",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testroot"))),
                ImmutableList<Predecessor>.Empty));
            var factGraph = builder.Build();
            factGraph.FactReferences.Should().ContainSingle()
                .Which.Hash.Should().Be("52bexk3CJadZJk31WH3KhvQAGr6CNHLdGIL+0vW7auWFznhfcpE/FKAQgC7syq+4aP78XgWhhJUevNoYyC25BA==");
        }

        [Fact]
        public void Builder_InOrder()
        {
            var builder = new FactGraphBuilder();
            builder.Add(Fact.Create(
                "Test.Root",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testroot"))),
                ImmutableList<Predecessor>.Empty));
            builder.Add(Fact.Create(
                "Test.Successor",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testsuccessor"))),
                ImmutableList.Create(
                    (Predecessor)new PredecessorSingle("root",
                        new FactReference("Test.Root", "52bexk3CJadZJk31WH3KhvQAGr6CNHLdGIL+0vW7auWFznhfcpE/FKAQgC7syq+4aP78XgWhhJUevNoYyC25BA==")))));
            var factGraph = builder.Build();
            Assert.Equal(2, factGraph.FactReferences.Count);
            Assert.Equal("Test.Root", factGraph.GetFact(factGraph.FactReferences[0]).Reference.Type);
            Assert.Equal("Test.Successor", factGraph.GetFact(factGraph.FactReferences[1]).Reference.Type);
        }

        [Fact]
        public void Builder_OutOfOrder()
        {
            var builder = new FactGraphBuilder();
            builder.Add(Fact.Create(
                "Test.Successor",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testsuccessor"))),
                ImmutableList.Create(
                    (Predecessor)new PredecessorSingle("root",
                        new FactReference("Test.Root", "52bexk3CJadZJk31WH3KhvQAGr6CNHLdGIL+0vW7auWFznhfcpE/FKAQgC7syq+4aP78XgWhhJUevNoYyC25BA==")))));
            builder.Add(Fact.Create(
                "Test.Root",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testroot"))),
                ImmutableList<Predecessor>.Empty));
            var factGraph = builder.Build();
            Assert.Equal(2, factGraph.FactReferences.Count);
            Assert.Equal("Test.Root", factGraph.GetFact(factGraph.FactReferences[0]).Reference.Type);
            Assert.Equal("Test.Successor", factGraph.GetFact(factGraph.FactReferences[1]).Reference.Type);
        }

        [Fact]
        public void Buidler_Incomplete()
        {
            var builder = new FactGraphBuilder();
            builder.Add(Fact.Create(
                "Test.Successor",
                ImmutableList.Create(
                    new Field("identifier",
                        new FieldValueString("testsuccessor"))),
                ImmutableList.Create(
                    (Predecessor)new PredecessorSingle("root",
                        new FactReference("Test.Root", "52bexk3CJadZJk31WH3KhvQAGr6CNHLdGIL+0vW7auWFznhfcpE/FKAQgC7syq+4aP78XgWhhJUevNoYyC25BA==")))));
            var graph = builder.Build();
            graph.FactReferences.Should().BeEmpty();
        }
    }
}
