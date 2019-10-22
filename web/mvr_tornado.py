import md5, time
import tornado.web, tornado.ioloop
from tornado.options import define, options

define("port", default=8472, help="run on the given port", type=int)

with open('bkgnd.jpg') as f:
    bkgnd_jpeg = f.read()

def load_scores():
    global scores, improvements
    scores = {}
    with open("scores", "r") as f:
        for line in f:
            line, comment = line.split('#')
            key, value = line.split(':')
            scores[int(key)] = int(value)
    improvements = {}
    for key, value in scores.items():
        header = str(key) + 'SeCrEt%s'
        for i in range(1, value):
            digest = md5.md5(header % i).hexdigest()
            improvements[digest] = key, i

def write_new_score(key, value):
    with open("scores", "a") as f:
        f.write('%d: %d\t\t# %s\n' % (key, value, time.ctime()))

load_scores()


class Application(tornado.web.Application):

    def __init__(self):
        handlers = [
            (r"/", IndexHandler),
            (r"/index.html", IndexHandler),
            (r"/([^/]+[.]jpg)", JpegHandler),
            (r"/score", ScoreHandler),
        ]
        super(Application, self).__init__(handlers)


class IndexHandler(tornado.web.RequestHandler):
    def get(self):
        self.render("index.html")

class JpegHandler(tornado.web.RequestHandler):
    def get(self, filename):
        self.set_header('Content-Type', 'image/jpeg')
        self.set_header('Cache-Control', 'max-age=8640000')
        self.write(bkgnd_jpeg)

class ScoreHandler(tornado.web.RequestHandler):
    def get(self):
        hexdigest = self.get_argument('h', '')
        if hexdigest in improvements:
            n1, s1 = improvements[hexdigest]
            if s1 < scores[n1]:
                scores[n1] = s1
                write_new_score(n1, s1)
        self.set_header('Content-Type', 'text/plain')
        self.set_header('Cache-Control', 'no-cache')
        self.write(u'\n'.join(['%s: %s' % keyval for keyval in scores.items()]))


def main(): 
    tornado.options.parse_command_line() 
    app = Application() 
    app.listen(options.port) 
    tornado.ioloop.IOLoop.current().start() 
 
if __name__ == "__main__": 
    main() 
