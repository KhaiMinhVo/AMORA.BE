db.Messages.updateMany(
  { ContentUrl: { $regex: '^https://amora-voice-bucket.s3.ap-southeast-1.amazonaws.com' } },
  [{
    $set: {
      ContentUrl: {
        $replaceOne: {
          input: "$ContentUrl",
          find: "https://amora-voice-bucket.s3.ap-southeast-1.amazonaws.com",
          replacement: "https://cdn.amora.pro.vn"
        }
      }
    }
  }]
);
