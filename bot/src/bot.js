const path = require('path')
const rabbit = require('./rabbit')
const logger = require('./logfactory').getLogger(path.basename(__filename))
const Twit = require('twit')

var T = new Twit({
  consumer_key: process.env.TWITTER_CONSUMER_KEY,
  consumer_secret: process.env.TWITTER_CONSUMER_SECRET,
  access_token: process.env.TWITTER_ACCESS_TOKEN,
  access_token_secret: process.env.TWITTER_ACCESS_TOKEN_SECRET,
  timeout_ms: 60 * 1000, // optional HTTP request timeout to apply to all requests.
  strictSSL: true // optional - requires SSL certificates to be valid.
})

async function start () {
  await rabbit.init()
  rabbit.listen(handle)
  logger.info('bot up and running')
}
start()

function handle (msg) {
  T.post('statuses/update', { status: buildTweet(msg) }, function (
    err,
    data,
    response
  ) {
    console.log(data)
  })
}
function buildTweet (msg) {
  const charLimit = 280
  const firstOption = `Jag hittade en ${msg.Positive ? 'positiv' : 'negativ'} mening innehållande nyckelordet ${msg.Keyword} i artikeln ${msg.ArticleUrl} 
  "${
    msg.Sentence
  }"`
  const secondOption = `${msg.Positive ? 'Positiv' : 'Negativ'} mening (nyckelord: ${msg.Keyword}, källa: ${msg.ArticleUrl}) 
  "${
    msg.Sentence
  }"`

  const thirdOption = `Jag hittade en ${msg.Positive ? 'positiv' : 'negativ'} mening innehållande nyckelordet ${msg.Keyword}. Mer info på https://mediaspin.johanhellgren.se. Allt fick inte plats i denna tweet`

  if (firstOption.length <= charLimit) {
    return firstOption
  } else if (secondOption.length <= charLimit) {
    return secondOption
  } else {
    return thirdOption
  }
}
