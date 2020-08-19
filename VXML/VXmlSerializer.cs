using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using VStorage;

namespace VXML
{
    internal class VXmlSerializer
    {
        // Constants
        private const long LEVEL_OFFSET = 0;
        private const long LEVEL_LENGTH = 2;

        private const long TYPE_OFFSET = LEVEL_OFFSET + LEVEL_LENGTH;
        private const long TYPE_LENGTH = 2;

        private const long NAME_L_OFFSET = TYPE_OFFSET + TYPE_LENGTH;
        private const long NAME_L_LENGTH = 2;

        private const long VALUE_L_OFFSET = NAME_L_OFFSET + NAME_L_LENGTH;
        private const long VALUE_L_LENGTH = 8;

        private const long CONT_L_OFFSET = VALUE_L_OFFSET + VALUE_L_LENGTH;
        private const long CONT_L_LENGTH = 8;

        private const long ATTR_NUMBER_OFFSET = VALUE_L_OFFSET + VALUE_L_LENGTH;
        private const long ATTR_NUMBER_LENGTH = 4;

        private const long RESERVE_OFFSET = ATTR_NUMBER_OFFSET + ATTR_NUMBER_LENGTH;
        private const long RESERVE_LENGTH = 16;

        private const long NAME_OFFSET = RESERVE_OFFSET + RESERVE_LENGTH;

        // Properties
        public short Level = 0;                         // Level
        public short Type = 0;                          // Type
        public string Name = "";                        // Name
        public string Value = "";                       // Value
        public byte[] Content = null;                   // Value
        
        public List<string> Attr_Names;                 // Attribute names

        public List<string> Attr_Values;                // Attribute values

        public List<string> Comment_Values;             // Comment values

        public List<string> Text_Values;                // Text values

        public List<string> Tag_Values;                 // Tag values


        // Private fields
        private short name_length = 0;                  // Name length
        private long value_length = 0;                  // Value length
        private long content_length = 0;                // Content length
        private int attr_number = 0;                    // Number of attrs
        private int comm_number = 0;                    // Number of comments
        private int text_number = 0;                    // Number of text nodes
        private int tag_number =  0;                    // Number of tag nodes

        private VSIO IO = null;

        public VXmlSerializer(VSIO io)
        {
            IO = io;
        }

        /// <summary>
        /// Serialize node
        /// </summary>
        /// <param name="node"></param>
        public void Serialize(VXmlNode node, short level)
        {
            Level = level;

            Type = node.NodeTypeCode;
            Name = node.Name;
            Value = node.Value;
            Content = null;

            VXmlContent ct = null;
            if (Type == DEFX.NODE_TYPE_CONTENT)
            {
                ct = (VXmlContent)node.GetNode(node.Id);
                Content = ct.ContentBytes;
            }

            VXmlAttributeCollection ac = node.Attributes;
            VXmlCommentCollection cc = node.CommentNodes;
            VXmlTextCollection tx = node.TextNodes;
            VXmlTagCollection tt = node.TagNodes;


            byte[] br = new byte[RESERVE_LENGTH];                   // Reserve
            
            // Fixed part
            IO.Write(-1, level);                 // +08(2) - level
            IO.Write(-1, Type);                  // +10(2) - node type
            IO.Write(-1, (short)Name.Length);    // +12(2) - name length
            IO.Write(-1, (long)Value.Length);    // +14(8) - value length
            IO.Write(-1, (long)((Content == null) ? 0 : Content.Length));   // +22(8) - content length
            IO.Write(-1, (int)ac.Count);         // +30(4)   Number of attrs
            IO.Write(-1, (int)cc.Count);         // +34(4)   Number of comments
            IO.Write(-1, (int)tx.Count);         // +34(4)   number of text nodes
            IO.Write(-1, (int)tt.Count);         // +38(4)   number of tags
            IO.Write(-1, ref br);                // +42(16)- reserve

            // Variable part
            IO.Write(-1, node.Name);        // Name

            if (Value.Length > 0)
                IO.Write(-1, Value);        // Value

            if (Content != null)
                IO.Write(-1, ref Content); // Content

            // Write attributes
            for (int i = 0; i < ac.Count; i++)
            {
                IO.Write(-1, (short)ac[i].Name.Length);       // Attr name length
                IO.Write(-1, ac[i].Name);                     // Attr name

                IO.Write(-1, (int)ac[i].Value.Length);       // Attr value length
                IO.Write(-1, ac[i].Value);                   // Attr value
            }
            
            // Write comments
            for (int i = 0; i < cc.Count; i++)
            {
                IO.Write(-1, (int)cc[i].Value.Length);       // Comment value length
                IO.Write(-1, cc[i].Value);                   // Comment value
            }

            // Write text
            for (int i = 0; i < tx.Count; i++)
            {
                IO.Write(-1, (int)tx[i].Value.Length);       // Text value length
                IO.Write(-1, tx[i].Value);                   // Text value
            }

            // Write tags
            for (int i = 0; i < tt.Count; i++)
            {
                IO.Write(-1, (int)tt[i].Value.Length);       // Tag value length
                IO.Write(-1, tt[i].Value);                   // Tag value
            }

        }

        /// <summary>
        /// DeSerialize node
        /// </summary>
        /// <param name="node"></param>
        public bool Deserialize()
        {
            Level = 0;
            Type = 0;
            Name = "";
            Value = "";
            name_length = 0;
            value_length = 0;
            content_length = 0;

            attr_number = 0;
            comm_number = 0;
            text_number = 0;
            tag_number = 0;

            Attr_Names = new List<string>();

            Attr_Values = new List<string>();

            Comment_Values = new List<string>();
            
            Text_Values = new List<string>();

            Tag_Values = new List<string>();


            if (IO.GetPosition() >= IO.GetLength())
                return false;

            Level = IO.ReadShort();                                       // 2 - level
            Type = IO.ReadShort();                                        // 2 - node type
            if ((Type < 1) | (Type >= DEFX.NODE_TYPE.Length))
                throw new Exception("INVALID TYPE!!!");
            name_length = IO.ReadShort();                                  // 2 - name length
            value_length = IO.ReadLong();                                  // 8 - value length  
            content_length = IO.ReadLong();                                // 8 - content length

            attr_number = IO.ReadInt();                                    // 4 - number of attrs

            comm_number = IO.ReadInt();                                    // 4 - number of comments

            text_number = IO.ReadInt();                                    // 4 - number of text nodes

            tag_number = IO.ReadInt();                                     // 4 - number of tag nodes

            IO.SetPosition(IO.GetPosition() + RESERVE_LENGTH);                                 // Shift offset
            Name = IO.ReadString(-1, (int)name_length);                  // Name

            if (value_length > 0)
                Value = IO.ReadString(-1, (int)value_length);            // Value
            else
                Value = "";

            if (content_length > 0)
                Content = IO.ReadBytes(-1, (int)content_length);         // Content
            else
                Content = null;

            for (int i = 0; i < attr_number; i++)
            {
                short lname = IO.ReadShort();
                string name = IO.ReadString(-1, (int)lname);

                int lvalue = IO.ReadInt();
                string value;
                if (lvalue > 0)
                    value = IO.ReadString(-1, (int)lvalue);
                else value = "";

                Attr_Names.Add(name);
                Attr_Values.Add(value);
            }


            for (int i = 0; i < comm_number; i++)
            {
                int lvalue = IO.ReadInt();
                string value;
                if (lvalue > 0)
                {
                    value = IO.ReadString(-1, (int)lvalue);
                    Comment_Values.Add(value);
                }
            }

            for (int i = 0; i < text_number; i++)
            {
                int lvalue = IO.ReadInt();
                string value;
                if (lvalue > 0)
                {
                    value = IO.ReadString(-1, (int)lvalue);
                    Text_Values.Add(value);
                }
            }

            for (int i = 0; i < tag_number; i++)
            {
                int lvalue = IO.ReadInt();
                string value;
                if (lvalue > 0)
                {
                    value = IO.ReadString(-1, (int)lvalue);
                    Tag_Values.Add(value);
                }
            }

            return true;
        }
    }
}
