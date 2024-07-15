# DNN_Blog_to_Text
Basic SQL script plus C# code to move DNN (DotNetNuke) Blogs to Markdown files for use in a JamStack CMS, like HUGO

This provides the basic infrastructure to read DNN (circa version 8?) Blogs, and write them to individual files with names based on their post date and EntryID. The files are written into directories based on the year published.

For my target of a HUGO based blog, I convert the data I find in the SQL Server DNN Blog tables to standard Front Matter (https://gohugo.io/content-management/front-matter/), which looks like this:

```
---
title: Nepal earthquake-- many similarities and one big difference
description: 
date: 2015-06-03T04:55:00Z
params:
   dnn_blog_ID: 5
   dnn_entry_ID: 379
   meta_title: Nepal earthquake-- many similarities and one big difference
   allow_comments: True
   display_copyright: False
   copyright: 
   permalink: https://myWebSite.org/en-us/Home/EntryId/379/Nepal-earthquake-many-similarities-and-one-big-difference
   image: 379_blog-image.png
   author: Unknown
categories: []
tags: [nepal, resources, preparation, resilience, Earthquake]
keywords: []
topics: []
draft: False
---
```

This is followed by the actual HTML of the blog entry. The only changes to this are:
- Conversion of entities (&nbsp;) to actual HTML tags
- Conversion of photo and attachment paths to another you can specify for your new blog

For reference, here are the DNN Blog Table fileds/columns that I was working from. You may have a different version:
```
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
```

I used 3 sequential SQL Scripts, but a SQL wiz could likely streamline these. Still, this processes some 200 blog entries in a few seconds.
```
-- 1.  Get the tags for each blog entry
DROP TABLE IF EXISTS aaTagsByEntryIDWithEnglish

SELECT EntryID, Blog_Tags.TagID, Tag
INTO aaTagsByEntryIDWithEnglish		
FROM   Blog_Entry_Tags INNER JOIN
        Blog_Tags ON Blog_Entry_Tags.TagID = Blog_Tags.TagID
        Order By EntryID";
```
then
```
--2.Concatenate tags(into one comma separated cell) for each blog entry
DROP TABLE IF EXISTS aaHorizTags

SELECT EntryID, STRING_AGG(CONVERT(NVARCHAR(max), Tag), ', ') as AllTags
    INTO aaHorizTags
    FROM aaTagsByEntryIDWithEnglish
    GROUP BY EntryID
    ORDER BY EntryID";
```
& finally
```
--3.Join blog entries with blog description and tags
-- DROP TABLE IF EXISTS aaTheWholeEnchilada

SELECT *
--INTO aaTheWholeEnchilada
FROM    Blog_Entries INNER JOIN
        Blog_Blogs ON Blog_Entries.BlogID = Blog_Blogs.BlogID INNER JOIN
        aaHorizTags ON aaHorizTags.EntryID = Blog_Entries.EntryID
--AS  aaTheWholeEnchilada
```

This sufficed for me, but you may likely have to do some more alterations for some of your blog entries if you used 'funky' formatting. Enjoy!
