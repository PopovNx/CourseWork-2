
FROM tensorflow/tensorflow:latest
RUN apt-get update && apt-get install -y openssh-server graphviz

RUN pip install keras
RUN pip install pydot
RUN pip install matplotlib
RUN mkdir /var/run/sshd

RUN useradd -m -d /home/keras_user -s /bin/bash keras_user && \
    echo 'keras_user:password' | chpasswd && \
    mkdir /home/keras_user/.ssh && \
    chmod 700 /home/keras_user/.ssh

RUN sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config && \
    sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/' /etc/ssh/sshd_config
EXPOSE 22
ENTRYPOINT ["bash", "-c", "/usr/sbin/sshd && tail -f /dev/null"]
