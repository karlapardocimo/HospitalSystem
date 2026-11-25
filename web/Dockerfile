# Aplicativo PHP (cliente da API). A imagem oficial já traz a extensão cURL.
FROM php:8.3-apache
COPY . /var/www/html/
RUN echo "date.timezone=America/Sao_Paulo" > /usr/local/etc/php/conf.d/timezone.ini
EXPOSE 80
