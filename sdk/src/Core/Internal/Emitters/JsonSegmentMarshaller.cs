//-----------------------------------------------------------------------------
// <copyright file="JsonSegmentMarshaller.cs" company="Amazon.com">
//      Copyright 2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
//
//      Licensed under the Apache License, Version 2.0 (the "License").
//      You may not use this file except in compliance with the License.
//      A copy of the License is located at
//
//      http://aws.amazon.com/apache2.0
//
//      or in the "license" file accompanying this file. This file is distributed
//      on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
//      express or implied. See the License for the specific language governing
//      permissions and limitations under the License.
// </copyright>
//-----------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Amazon.Runtime;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Core.Internal.Emitters
{
    /// <summary>
    /// Convert a segment into JSON string
    /// </summary>
    public class JsonSegmentMarshaller : ISegmentMarshaller
    {
        private const string ProtocolHeader = "{\"format\":\"json\",\"version\":1}";
        private const char ProtocolDelimiter = '\n';

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSegmentMarshaller"/> class.
        /// </summary>
        public JsonSegmentMarshaller()
        {
            JsonMapper.RegisterExporter<Entity>(EntityExporter);
            JsonMapper.RegisterExporter<ExceptionDescriptor>(ExceptionDescriptorExporter);
            JsonMapper.RegisterExporter<Cause>(CauseExporter);
            JsonMapper.RegisterExporter<Annotations>(AnnotationsExporter);
            JsonMapper.RegisterExporter<HttpMethod>(HttpMethodExporter);
            JsonMapper.RegisterExporter<ConstantClass>(ConstantClassExporter);
        }

        /// <summary>
        /// Marshall the segment into JSON string
        /// </summary>
        /// <param name="segment">The segment to parse</param>
        /// <returns>The JSON string parsed from given segment</returns>
        public string Marshall(Entity segment)
        {
            return ProtocolHeader + ProtocolDelimiter + JsonMapper.ToJson(segment);
        }

        private static void EntityExporter(Entity entity, JsonWriter writer)
        {
            writer.WriteObjectStart();

            WriteEntityFields(entity, writer);

            var segment = entity as Segment;
            if (segment != null)
            {
                WriteSegmentFields(segment, writer);
            }

            var subsegment = entity as Subsegment;
            if (subsegment != null)
            {
                WriteSubsegmentFields(subsegment, writer);    
            }

            writer.WriteObjectEnd();
        }

        private static void WriteEntityFields(Entity entity, JsonWriter writer)
        {
            if (!string.IsNullOrEmpty(entity.TraceId))
            {
                writer.WritePropertyName("trace_id");
                writer.Write(entity.TraceId);
            }

            writer.WritePropertyName("id");
            writer.Write(entity.Id);

            writer.WritePropertyName("start_time");
            writer.Write(entity.StartTime);

            writer.WritePropertyName("end_time");
            writer.Write(entity.EndTime);

            if (entity.ParentId != null)
            {
                writer.WritePropertyName("parent_id");
                writer.Write(entity.ParentId);
            }

            writer.WritePropertyName("name");
            writer.Write(entity.Name);

            if (entity.IsSubsegmentsAdded)
            {
                writer.WritePropertyName("subsegments");
                JsonMapper.ToJson(entity.Subsegments, writer);
            }

            if (entity.IsAwsAdded)
            {
                writer.WritePropertyName("aws");
                JsonMapper.ToJson(entity.Aws, writer);
            }

            if (entity.HasFault)
            {
                writer.WritePropertyName("fault");
                writer.Write(entity.HasFault);
            }

            if (entity.HasError)
            {
                writer.WritePropertyName("error");
                writer.Write(entity.HasError);
            }

            if (entity.IsThrottled)
            {
                writer.WritePropertyName("throttle");
                writer.Write(true);
            }

            if (entity.Cause != null)
            {
                writer.WritePropertyName("cause");
                JsonMapper.ToJson(entity.Cause, writer);
            }

            if (entity.IsAnnotationsAdded)
            {
                writer.WritePropertyName("annotations");
                JsonMapper.ToJson(entity.Annotations, writer);
            }

            if (entity.IsMetadataAdded)
            {
                writer.WritePropertyName("metadata");
                JsonMapper.ToJson(entity.Metadata, writer);
            }

            if (entity.IsHttpAdded)
            {
                writer.WritePropertyName("http");
                JsonMapper.ToJson(entity.Http, writer);
            }

            if (entity.IsSqlAdded)
            {
                writer.WritePropertyName("sql");
                JsonMapper.ToJson(entity.Sql, writer);
            }
        }

        private static void WriteSegmentFields(Segment segment, JsonWriter writer)
        {
            if (segment.Origin != null)
            {
                writer.WritePropertyName("origin");
                writer.Write(segment.Origin);
            }

            if (segment.IsServiceAdded)
            {
                writer.WritePropertyName("service");
                JsonMapper.ToJson(segment.Service, writer);
            }
        }

        private static void WriteSubsegmentFields(Subsegment subsegment, JsonWriter writer)
        {
            if (subsegment.Type != null)
            {
                writer.WritePropertyName("type");
                writer.Write(subsegment.Type);
            }

            if (subsegment.Namespace != null)
            {
                writer.WritePropertyName("namespace");
                JsonMapper.ToJson(subsegment.Namespace, writer);
            }

            if (subsegment.IsPrecursorIdAdded)
            {
                writer.WritePropertyName("precursor_ids");
                JsonMapper.ToJson(subsegment.PrecursorIds.ToArray(), writer);
            }
        }

        private static void CauseExporter(Cause cause, JsonWriter writer)
        {
            // Propagating faults (e.g. exceptions) can refer to the local root cause exception with its ID rather than duplicating the exceptions.
            //     "cause" : "4fe5fbae3f9e29c1"
            if (cause.ReferenceExceptionId != null)
            {
                writer.Write(cause.ReferenceExceptionId);
                return;
            }

            writer.WriteObjectStart();

            if (cause.WorkingDirectory != null)
            {
                writer.WritePropertyName("working_directory");
                writer.Write(cause.WorkingDirectory);
            }

            if (cause.Paths != null)
            {
                writer.WritePropertyName("paths");
                JsonMapper.ToJson(cause.Paths, writer);
            }

            if (cause.IsExceptionAdded)
            {
                writer.WritePropertyName("exceptions");
                JsonMapper.ToJson(cause.ExceptionDescriptors, writer);
            }

            writer.WriteObjectEnd();
        }

        private static void ExceptionDescriptorExporter(ExceptionDescriptor descriptor, JsonWriter writer)
        {
            writer.WriteObjectStart();  // exception
            writer.WritePropertyName("id");
            writer.Write(descriptor.Id);

            writer.WritePropertyName("message");
            writer.Write(descriptor.Message);

            writer.WritePropertyName("type");
            writer.Write(descriptor.Type);

            writer.WritePropertyName("remote");
            writer.Write(descriptor.Remote);

            writer.WritePropertyName("stack");

            writer.WriteArrayStart();   // stack

            StackFrame[] frames = descriptor.Stack;
            if (frames != null)
            {
                foreach (StackFrame frame in frames)
                {
                    writer.WriteObjectStart();  // trace
                    writer.WritePropertyName("path");
                    writer.Write(frame.GetFileName());
                    writer.WritePropertyName("line");
                    writer.Write(frame.GetFileLineNumber());
                    writer.WritePropertyName("label");
                    MethodBase method = frame.GetMethod();
                    string label = method.Name;
                    if (method.ReflectedType != null)
                    {
                        label = method.ReflectedType.FullName + "." + label;
                    }

                    writer.Write(label);
                    writer.WriteObjectEnd();    // trace
                }
            }

            writer.WriteArrayEnd();     // stack

            if (descriptor.Truncated > 0)
            {
                writer.WritePropertyName("truncated");
                writer.Write(descriptor.Truncated);
            }

            if (descriptor.Cause != null)
            {
                writer.WritePropertyName("cause");
                writer.Write(descriptor.Cause);
            } 
            
            writer.WriteObjectEnd();    // exception
        }

        private static void AnnotationsExporter(Annotations annotations, JsonWriter writer)
        {
            writer.WriteObjectStart();
            foreach (var annotation in annotations)
            {
                writer.WritePropertyName(annotation.Key);
                JsonMapper.ToJson(annotation.Value, writer);
            }

            writer.WriteObjectEnd();
        }

        private static void HttpMethodExporter(HttpMethod method, JsonWriter writer)
        {
            writer.Write(method.Method);
        }

        private static void ConstantClassExporter(ConstantClass constantClass, JsonWriter writer)
        {
            writer.Write(constantClass.Value);
        }
    }
}
