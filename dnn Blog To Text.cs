/// DNN Blog to Markdown files
/// Hugo/GoldMark accepts HTML within Markdown files, so we convert what we can from the DNN SQL Server DB to Front Matter
/// The main blog entries are just converted back to HTML (without HTML entity codes!)
/// ©VashonSoftware.com, 2024 under an MIT LIcense

using System;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml.Linq;

internal class DNNBlog2Markdown
{
    private static void Main()
    {
        // string filePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string filePath = "C:\\DNNblog";

        // TODO:     static void writeAuthorsFromDB(string filePath);

        writeDNNBlogsToMarkdownFiles(filePath);


        // Not perfect, but sufficient?
        static string StripHTML(string input)
        {
            if (input.Equals(null) || input.Equals(""))
                return " ";
            input = System.Web.HttpUtility.HtmlDecode(input)
                .Replace(":", "--");
            if (input.Contains('\n'))
            {
                input = input.Split('\n')[0];
            }
            return Regex.Replace(input, "<.*?>", String.Empty);
        }


        static void writeDNNBlogsToMarkdownFiles(string filePath)
        {
            /*
            Blog Tables used by DNN Version ~8
                Entries:   BlogID EntryID Title Entry   AddedDate Published   Description AllowComments   DisplayCopyright Copyright   PermaLink Blog_Comments   Blog_Entry_Categories Blog_Entry_Tags
                Categories: CatID Category    Slug ParentID    PortalID Blog_Entry_Categories   Portals
                Comments: CommentID EntryID UserID Comment AddedDate Title   Approved Author  Website Email   Blog_Entries
                Blogs: PortalID BlogID  UserID Title   Description Public  AllowComments AllowAnonymous  LastEntry Created ShowFullName DateFormat  Culture TimeZone    ParentBlogID Syndicated  SyndicateIndependant SyndicationURL  SyndicationEmail EmailNotification   AllowTrackbacks AutoTrackback   MustApproveComments MustApproveAnonymous    MustApproveTrackbacks UseCaptcha  Portals Users
                Entry_Categories: EntryCatID EntryID CatID Blog_Categories Blog_Entries
                Entry_Tags: EntryTagID EntryID TagID Blog_Entries    Blog_Tags
                MetaWeblogData: TempInstallUrl
                Settings: PortalID Key Value TabID   Portals
                Tags: TagID Tag Slug Active  PortalID Blog_Entry_Tags Portals
            */

            string newFileName;
            DateTime incomingDate;
            string incomingDateS;

            string sqlJoin1 =
                @"-- 1.  Get the tags for each blog entry
                DROP TABLE IF EXISTS aaTagsByEntryIDWithEnglish

                SELECT EntryID, Blog_Tags.TagID, Tag
                INTO aaTagsByEntryIDWithEnglish		
                FROM   Blog_Entry_Tags INNER JOIN
		                Blog_Tags ON Blog_Entry_Tags.TagID = Blog_Tags.TagID
		                Order By EntryID";

            string sqlJoin2 =
                @"--2.Concatenate tags(into one comma separated cell) for each blog entry
                DROP TABLE IF EXISTS aaHorizTags

                SELECT EntryID, STRING_AGG(CONVERT(NVARCHAR(max), Tag), ', ') as AllTags
                    INTO aaHorizTags
                    FROM aaTagsByEntryIDWithEnglish
                    GROUP BY EntryID
                    ORDER BY EntryID";

            string sqlJoin3 =
                @"--3.Join blog entries with blog description and tags
                DROP TABLE IF EXISTS aaTheWholeEnchilada

                SELECT *
                --INTO aaTheWholeEnchilada
                FROM    Blog_Entries INNER JOIN
                        Blog_Blogs ON Blog_Entries.BlogID = Blog_Blogs.BlogID INNER JOIN
                        aaHorizTags ON aaHorizTags.EntryID = Blog_Entries.EntryID
";
            //              AS  aaTheWholeEnchilada";

            string entryS, entry, title, desc, dateS;
            string delim = "-";

            //create connection
            SqlCommand comm = new SqlCommand();
            comm.Connection = new SqlConnection(@"Server=***mySQLServer***;Database=***myDataBase***;uid=sa;pwd=******;");
            comm.CommandText = sqlJoin1;
            comm.Connection.Open();
            SqlDataReader sqlReader = comm.ExecuteReader();
            sqlReader.Close();

            comm.CommandText = sqlJoin2;
            sqlReader = comm.ExecuteReader();
            sqlReader.Close();

            comm.CommandText = sqlJoin3;
            sqlReader = comm.ExecuteReader();

            // Create newMarkdownFile to write to.  (If exists, it will be overwritten.)
            while (sqlReader.Read())
            {
                if (sqlReader["AddedDate"].Equals(null))
                    incomingDate = new DateTime(2000, 1, 1, 5, 0, 0);
                else
                {
                    incomingDateS = sqlReader["AddedDate"].ToString();
                    incomingDate = DateTime.Parse(incomingDateS);
                }

                // New Markdown file named by date and EntryID
                newFileName = filePath + "\\" + incomingDate.Year + "\\" + incomingDate.Year + delim + incomingDate.Month + delim + incomingDate.Day + "_Entry" + sqlReader["EntryID"] + ".md";

                if (!Directory.Exists(Path.GetDirectoryName(newFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileName));
                }

                using (StreamWriter newMarkdownFile = new StreamWriter(newFileName, false))
                {
                    // Write Forward Matter: https://gohugo.io/content-management/front-matter/
                    newMarkdownFile.WriteLine("---");
                    title = StripHTML(sqlReader["Title"].ToString());
                    newMarkdownFile.WriteLine("title: " + title);

                    desc = StripHTML(sqlReader["Description"].ToString());
                    if (desc.Equals(null) || desc.Equals("") || desc.Equals(" "))
                        newMarkdownFile.WriteLine("description: " + title);
                    else
                    {
                        newMarkdownFile.WriteLine("description: " + desc);
                    }

                    dateS = incomingDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    newMarkdownFile.WriteLine("date: " + dateS); // desired format: 2022 - 04 - 04T05: 00:00Z

                    // Write custom/non-standard values in a params section
                    newMarkdownFile.WriteLine("params:");
                    newMarkdownFile.WriteLine("   dnn_blog_ID: " + sqlReader["BlogID"]);
                    newMarkdownFile.WriteLine("   dnn_entry_ID: " + sqlReader["EntryID"]);
                    newMarkdownFile.WriteLine("   meta_title: " + title);
                    newMarkdownFile.WriteLine("   allow_comments: " + sqlReader["AllowComments"]);
                    newMarkdownFile.WriteLine("   display_copyright: " + sqlReader["DisplayCopyright"]);
                    newMarkdownFile.WriteLine("   copyright: " + sqlReader["Copyright"]);

                    entryS = "   permalink: " + sqlReader["PermaLink"].ToString();
                    newMarkdownFile.WriteLine(entryS);

                    newMarkdownFile.WriteLine("   image: " + sqlReader["EntryID"] + "_blog-image.png"); // or use a default placeholder image
                    if (sqlReader["BlogID"].Equals(1))
                        newMarkdownFile.WriteLine("   author: John Cornelison"); // special for my needs
                    else
                        newMarkdownFile.WriteLine("   author: Unknown");   // TODO: We've Author's UserID (from dbo.blogAuthors table), but to get author's name: do another join with UserID table...

                    newMarkdownFile.WriteLine("categories: []");
                    newMarkdownFile.WriteLine("tags: [" + sqlReader["AllTags"] + "]");
                    newMarkdownFile.WriteLine("keywords: []"); // special for my needs
                    newMarkdownFile.WriteLine("topics: []"); // special for my needs
                    if (sqlReader["Published"].Equals(true))
                        newMarkdownFile.WriteLine("draft: False");
                    else
                        newMarkdownFile.WriteLine("draft: True");
                    newMarkdownFile.WriteLine("---");
                    newMarkdownFile.WriteLine();
                    // Done with Front Matter

                    // Now write the 'real' blog content, which is allowed to be regular HTML
                    entryS = sqlReader["Entry"].ToString();

                    // Remap paths to blog images & attachments: /Portals/1/Blog/Files to new path
                    entryS = entryS.Replace("/Portals/1/Blog/Files", "/images/dnnBlog");

                    entry = System.Web.HttpUtility.HtmlDecode(entryS);
                    newMarkdownFile.WriteLine(entry);

                    newMarkdownFile.Close();
                }
            }

            sqlReader.Close();
            comm.Connection.Close();
        }
    }
}